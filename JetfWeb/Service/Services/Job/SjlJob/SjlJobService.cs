using Dapper;
using Newtonsoft.Json;
using Service.Extensions;
using Service.Helpers;
using Service.Services.Job.SjlJob.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Job.SjlJob
{
    /// <summary>
    /// 金祥富稅金資料傳送服務
    /// </summary>
    public class SjlJobService : _BaseService
    {
        //測試
        //private const string ApiUrl = "http://test.haysilk.com:7070/wmsbackend/openapi/v/invoke";
        //正式
        private const string ApiUrl = "https://www.haysilk.com/wmsbackend/openapi/v/invoke";
        private const string TokenSid = "getToken";
        private const string ReceiveSid = "RECEIVE_JF_TAX";
        private const string TokenRequestBody = "eyJhcHBJZCI6ImppZV9mZW5nIiwicGFzc3dvcmQiOiJKJmYxNzA2MDQyNSEifQ==";
        private const int MaxRetryAttempts = 3;
        private const int MessageMaxLength = 500;
        private const string AccessTokenCacheKey = "SjlJobService.JhfAccessToken";
        private static readonly TimeSpan TokenCacheDuration = TimeSpan.FromMinutes(18);

        /// <summary>
        /// 執行金祥富稅金資料傳送
        /// </summary>
        /// <returns></returns>
        public async Task RunJhfTaxJobAsync()
        {
            var modifyTime = DateTime.Today.AddDays(-1).AddHours(22);
            var taxList = GetJhfTaxList(modifyTime);

            if (!taxList.Any())
            {
                return;
            }

            foreach (var taxItem in taxList)
            {
                var executionResult = await SendTaxListWithRetryAsync(taxItem);
                await SaveResponseAsync(taxItem, executionResult);
            }
        }

        /// <summary>
        /// 取得金祥富稅金資料
        /// </summary>
        /// <param name="modifyTime">最後異動時間起點</param>
        /// <returns></returns>
        public List<JhfTaxQueryModel> GetJhfTaxList(DateTime modifyTime)
        {
            var sql = @"
WITH CLEARANCE_TAX AS
(
    SELECT MAIN_NUMBER,
           BAG_NUMBER,
           TAX_AMOUNT,
           TAX_NUMBER
    FROM DATA_CENTER.dbo.CLEARANCE_TAX
    WHERE MODIFY_TIME >= @MODIFY_TIME
      AND DATA_TYPE NOT IN ('FTZ','TACT')
),
SEA_ORDER_ORIGINAL AS
(
    SELECT MAINNUMBER,
           BL_NO
    FROM DATA_CENTER.dbo.SEA_ORDER_ORIGINAL
    WHERE DESPATCH_NAME IN ('CN00165','CN00173')
      AND GW > 0
)
SELECT a.MAIN_NUMBER AS MainNumber,
       a.BAG_NUMBER AS BagNumber,
       a.TAX_NUMBER AS TaxNumber,
       a.TAX_AMOUNT AS TaxAmount
FROM CLEARANCE_TAX a
JOIN SEA_ORDER_ORIGINAL b
  ON a.MAIN_NUMBER = b.MAINNUMBER
 AND a.BAG_NUMBER = b.BL_NO
WHERE NOT EXISTS
(
    SELECT 1
    FROM [jetf].[dbo].[JhfTaxResponse] r
    WHERE r.MainNumber = a.MAIN_NUMBER
      AND r.BagNumber = a.BAG_NUMBER
      AND r.TaxNumber = a.TAX_NUMBER
      AND r.TaxAmount = a.TAX_AMOUNT
      AND r.Code = '0'
)";

            return conn.Query<JhfTaxQueryModel>(sql, new
            {
                MODIFY_TIME = modifyTime
            }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 以重試機制傳送金祥富稅金資料
        /// </summary>
        /// <param name="taxItem">稅金資料</param>
        /// <returns>執行結果</returns>
        private async Task<JhfTaxExecutionResultModel> SendTaxListWithRetryAsync(JhfTaxQueryModel taxItem)
        {
            int retryCount = 0;
            JhfTaxExecutionResultModel executionResult = null;

            while (retryCount < MaxRetryAttempts)
            {
                try
                {
                    // Step1：先取得可用的 accessToken，20 分鐘內重複使用。
                    var accessToken = await GetValidAccessTokenAsync();
                    if (string.IsNullOrWhiteSpace(accessToken))
                    {
                        return new JhfTaxExecutionResultModel
                        {
                            IsSuccess = false,
                            Code = null,
                            Message = "Token API 未回傳 accessToken"
                        };
                    }

                   // Step2：使用 accessToken 將單筆稅金資料送至 WMS Backend。
                    var taxResponse = await SendTaxAsync(accessToken, taxItem);
                    executionResult = BuildTaxExecutionResult(taxResponse);
             
                }
                catch (Exception ex)
                {
                    executionResult = new JhfTaxExecutionResultModel
                    {
                        IsSuccess = false,
                        Code = null,
                        Message = ex.Message.Truncate(MessageMaxLength)
                    };
                }

                if (executionResult != null && executionResult.IsSuccess)
                {
                    return executionResult;
                }

                retryCount++;

                if (retryCount < MaxRetryAttempts)
                {
                    await Task.Delay(1000);
                }
            }

            return executionResult ?? new JhfTaxExecutionResultModel
            {
                IsSuccess = false,
                Code = null,
                Message = "金祥富稅金傳送失敗".Truncate(MessageMaxLength)
            };
        }

        /// <summary>
        /// 取得可重複使用的 accessToken
        /// </summary>
        /// <returns>accessToken</returns>
        private async Task<string> GetValidAccessTokenAsync()
        {
            if (CacheHelper.Exist(AccessTokenCacheKey))
            {
                return CacheHelper.Get<string>(AccessTokenCacheKey);
            }

            // Step1：當本機沒有 token 或 token 已過期時，重新呼叫 Token API。
            var tokenResponse = await GetAccessTokenAsync();
            if (!IsTokenSuccess(tokenResponse))
            {
                ClearCachedAccessToken();
                return null;
            }

            // Step2：成功後快取 token 與到期時間，供後續 18 分鐘內重複使用。
            CacheHelper.Set(
                AccessTokenCacheKey,
                tokenResponse.Data,
                new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now.Add(TokenCacheDuration)
                });

            return tokenResponse.Data;
        }

        /// <summary>
        /// 呼叫 Token API 取得 accessToken
        /// </summary>
        /// <returns>Token API 回應</returns>
        private async Task<JhfTaxResponseModel> GetAccessTokenAsync()
        {
            var request = new JhfTaxApiRequestModel
            {
                Sid = TokenSid,
                Body = TokenRequestBody,
                DateTime = FormatRequestDateTime(DateTime.Now)
            };

            return await PostAsync<JhfTaxApiRequestModel, JhfTaxResponseModel>(request);
        }

        /// <summary>
        /// 清除目前快取中的 accessToken
        /// </summary>
        private void ClearCachedAccessToken()
        {
            CacheHelper.Remove(AccessTokenCacheKey);
        }

        /// <summary>
        /// 呼叫稅金接收 API 傳送稅金資料
        /// </summary>
        /// <param name="accessToken">Token API 回傳的 accessToken</param>
        /// <param name="taxItem">要傳送的單筆稅金資料</param>
        /// <returns>稅金 API 回應</returns>
        private async Task<JhfTaxResponseModel> SendTaxAsync(string accessToken, JhfTaxQueryModel taxItem)
        {
            // Step1：先依 API 規格組成單筆 taxList JSON 內容。
            var payload = new JhfTaxPayloadModel
            {
                TaxList = new List<JhfTaxPayloadItemModel>
                {
                    new JhfTaxPayloadItemModel
                    {
                        BagNo = taxItem.BagNumber,
                        TaxNo = taxItem.TaxNumber,
                        Tax = taxItem.TaxAmount
                    }
                }
            };

            // Step2：將單筆 taxList JSON 轉成 Base64 後放入 body，再連同 accessToken 一起送出。
            var request = new JhfTaxSendRequestModel
            {
                AccessToken = accessToken,
                Sid = ReceiveSid,
                Body = EncodeBase64Json(payload),
                DateTime = FormatRequestDateTime(DateTime.Now)
            };

            return await PostAsync<JhfTaxSendRequestModel, JhfTaxResponseModel>(request);
        }

        /// <summary>
        /// 送出 HTTP POST 請求並將回應轉為指定型別
        /// </summary>
        /// <typeparam name="TRequest">請求模型型別</typeparam>
        /// <typeparam name="TResponse">回應模型型別</typeparam>
        /// <param name="request">請求內容</param>
        /// <returns>反序列化後的回應模型</returns>
        private async Task<TResponse> PostAsync<TRequest, TResponse>(TRequest request)
        {
            using (var client = new HttpClient())
            {
                var jsonContent = JsonConvert.SerializeObject(request);
                using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                using (var response = await client.PostAsync(ApiUrl, content))
                {
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TResponse>(responseContent);
                }
            }
        }

        /// <summary>
        /// 判斷 Token API 是否成功取得 accessToken
        /// </summary>
        /// <param name="tokenResponse">Token API 回應</param>
        /// <returns>是否成功</returns>
        private bool IsTokenSuccess(JhfTaxResponseModel tokenResponse)
        {
            return tokenResponse != null
                && tokenResponse.Code == "0"
                && !string.IsNullOrWhiteSpace(tokenResponse.Data);
        }

        /// <summary>
        /// 建立 Token API 失敗時的執行結果
        /// </summary>
        /// <param name="tokenResponse">Token API 回應</param>
        /// <returns>執行結果</returns>
        private JhfTaxExecutionResultModel BuildTokenFailureResult(JhfTaxResponseModel tokenResponse)
        {
            var message = tokenResponse?.Message;
            if (tokenResponse != null && tokenResponse.Code == "0" && string.IsNullOrWhiteSpace(tokenResponse.Data))
            {
                message = "Token API 未回傳 accessToken";
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Token API 呼叫失敗";
            }

            return new JhfTaxExecutionResultModel
            {
                IsSuccess = false,
                Code = tokenResponse?.Code,
                Message = message.Truncate(MessageMaxLength)
            };
        }

        /// <summary>
        /// 解析稅金 API 回應並轉成統一執行結果
        /// </summary>
        /// <param name="taxResponse">稅金 API 回應</param>
        /// <returns>執行結果</returns>
        private JhfTaxExecutionResultModel BuildTaxExecutionResult(JhfTaxResponseModel taxResponse)
        {
            if (taxResponse == null)
            {
                return new JhfTaxExecutionResultModel
                {
                    IsSuccess = false,
                    Code = null,
                    Message = "稅金 API 未回傳內容"
                };
            }

            if (taxResponse.Code != "0")
            {
                return new JhfTaxExecutionResultModel
                {
                    IsSuccess = false,
                    Code = taxResponse.Code,
                    Message = (taxResponse.Message ?? "稅金 API 呼叫失敗").Truncate(MessageMaxLength)
                };
            }

            if (string.IsNullOrWhiteSpace(taxResponse.Data))
            {
                return new JhfTaxExecutionResultModel
                {
                    IsSuccess = false,
                    Code = taxResponse.Code,
                    Message = "稅金 API 未回傳業務結果"
                };
            }

            try
            {
                // Step1：當外層 code=0 時，繼續解析 data 內的業務結果。
                var businessResponse = JsonConvert.DeserializeObject<JhfTaxBusinessResponseModel>(taxResponse.Data);
                if (businessResponse != null && businessResponse.Code.HasValue)
                {
                    // Step2：依業務 code 判斷是否真正成功，並統一整理要回寫的訊息。
                    var isSuccess = businessResponse.Code.Value == 200;
                    var message = businessResponse.Msg;

                    if (string.IsNullOrWhiteSpace(message))
                    {
                        message = isSuccess ? "操作成功" : taxResponse.Data;
                    }

                    return new JhfTaxExecutionResultModel
                    {
                        IsSuccess = isSuccess,
                        Code = taxResponse.Code,
                        Message = (isSuccess ? "操作成功" : message).Truncate(MessageMaxLength)
                    };
                }
            }
            catch (JsonException)
            {
            }

            return new JhfTaxExecutionResultModel
            {
                IsSuccess = true,
                Code = taxResponse.Code,
                Message = "操作成功"
            };
        }

        /// <summary>
        /// 將執行結果回寫至 JhfTaxResponse 資料表
        /// </summary>
        /// <param name="taxItem">原始單筆稅金資料</param>
        /// <param name="executionResult">執行結果</param>
        /// <returns></returns>
        private async Task SaveResponseAsync(JhfTaxQueryModel taxItem, JhfTaxExecutionResultModel executionResult)
        {
            if (taxItem == null)
            {
                return;
            }

            var row = new
            {
                taxItem.MainNumber,
                taxItem.BagNumber,
                taxItem.TaxNumber,
                taxItem.TaxAmount,
                Code = executionResult?.Code,
                Message = executionResult?.Message.Truncate(MessageMaxLength)
            };

            var sql = @"
INSERT INTO [jetf].[dbo].[JhfTaxResponse]
(
    MainNumber,
    BagNumber,
    TaxNumber,
    TaxAmount,
    Code,
    Message
)
VALUES
(
    @MainNumber,
    @BagNumber,
    @TaxNumber,
    @TaxAmount,
    @Code,
    @Message
)";

            await conn.ExecuteAsync(sql, row);
        }

        /// <summary>
        /// 將物件序列化為 JSON 後再轉成 Base64 字串
        /// </summary>
        /// <param name="value">要轉換的物件</param>
        /// <returns>Base64 字串</returns>
        private string EncodeBase64Json(object value)
        {
            var json = JsonConvert.SerializeObject(value);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// 將時間格式化為 API 要求的日期字串格式
        /// </summary>
        /// <param name="dateTime">日期時間</param>
        /// <returns>格式化後字串</returns>
        private string FormatRequestDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}