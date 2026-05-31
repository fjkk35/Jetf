using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Service.Extensions;
using Service.Models;
using Service.Services.Ezway.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Service.Services.Ezway
{
    /// <summary>
    /// Ezway/ECCS 整合服務，負責登入、驗證碼、單筆查詢、整批查詢與匯出。
    /// </summary>
    public class EzwayService : _BaseService
    {
        private const string BaseUrl = "https://eccs.tradevan.com.tw/APECCS/ezway/";
        private const string AnonymousTokenUrl = BaseUrl + "auth/token";
        private const string LoginCaptchaUrl = BaseUrl + "v1/system/verfiryCode";
        private const string TermsPreviewUrl = BaseUrl + "v1/system/web_get_announcement";
        private const string TermsAgreeUrl = BaseUrl + "v1/system/web_announcement";
        private const string LoginUrl = BaseUrl + "wlogin";
        private const string QuerySettingUrl = BaseUrl + "v1/system/query/setting";
        private const string SingleQueryUrl = BaseUrl + "v4/realname/preverify-result";
        private const string BatchQueryUrl = BaseUrl + "v1/realname/preverify-result-batch";
        private const string SignSecret = "+xH9x!&";
        private const string SignChars = "0123456789abcdefghij";
        private const string QueryDecryptKeyBase64 = "vfqkS9So5y5CcyVCWhFYLTqlw27lvYhVo0QT+Hhbaa4=";
        private const string QueryDecryptIvText = "NR55MPkVQH5YIxcm";

        private const string SessionAnonymousTokenKey = "Ezway_AnonymousToken";
        private const string SessionJwtTokenKey = "Ezway_JwtToken";
        private const string SessionUserIdKey = "Ezway_UserId";
        private const string SessionBrokerBanKey = "Ezway_BrokerBan";

        private static readonly byte[] QueryDecryptKey = Convert.FromBase64String(QueryDecryptKeyBase64);
        private static readonly byte[] QueryDecryptIv = Encoding.UTF8.GetBytes(QueryDecryptIvText);
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly IReadOnlyDictionary<string, string> IsReplyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "00", "申報相符" },
            { "01", "無購買此貨物遭冒名申報" },
            { "02", "申報貨名不符" },
            { "03", "申報價格不符" },
            { "20", "移民署回復居留證無效" },
            { "21", "報單號碼格式錯誤" },
            { "22", "報關箱號錯誤(包括不存在、停業、註銷)" },
            { "23", "其他" },
            { "99", "其他" },
            { "1G", "集運商欄位不可空白" },
            { "1F", "集運商代碼錯誤 / 帳號及箱號不一致" }
        };
        private static readonly IReadOnlyDictionary<string, string> AuthorizeReplyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "00", "核准" },
            { "01", "移民署回復居留證無效" },
            { "02", "報關箱號錯誤(包括不存在、停業、註銷)" },
            { "03", "其他" },
            { "99", "寫入已接收到資料(僅限居留證)" }
        };
        private static readonly string[] AcceptedDateFormats =
        {
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMdd",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/MM/dd HH:mm",
            "yyyy/MM/dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy/MM/ddTHH:mm:ss",
            "HHmmss",
            "HH:mm:ss",
            "HHmm"
        };
        private static readonly HashSet<string> FullyMaskedLogFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authorization",
            "token",
            "userPwd",
            "password",
            "captcha",
            "code",
            "image",
            "captchaImageBase64",
            "captchaCode",
            "sign"
        };
        private static readonly HashSet<string> PartiallyMaskedLogFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "idNo",
            "telNo"
        };
        private const int MaxLoggedArrayPreviewCount = 5;
        private const int MaxLoggedStringLength = 240;
        private const int SingleQueryMaxCount = 10;
        private const string NoDataMessage = "查無相關資料";
        private static readonly IReadOnlyDictionary<string, string> ApiLogNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { AnonymousTokenUrl, "auth/token 取得匿名 Token" },
            { LoginCaptchaUrl, "v1/system/verfiryCode 取得登入驗證碼" },
            { TermsPreviewUrl, "v1/system/web_get_announcement 取得服務條款" },
            { TermsAgreeUrl, "v1/system/web_announcement 同意服務條款" },
            { LoginUrl, "wlogin 業者登入" },
            { QuerySettingUrl, "v1/system/query/setting 取得查詢驗證碼設定" },
            { SingleQueryUrl, "v4/realname/preverify-result 單筆查詢" },
            { BatchQueryUrl, "v1/realname/preverify-result-batch 整批查詢" }
        };

        /// <summary>
        /// 建立 Ezway 服務實例。
        /// </summary>
        public EzwayService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 取得 Ezway 頁面初始化狀態。
        /// </summary>
        public async Task<ResponseModel> GetPageStateAsync()
        {
            try
            {
                // 已登入時直接沿用既有 session 取查詢驗證碼；若失效則回到登入流程。
                if (HasAuthenticatedSession())
                {
                    try
                    {
                        return new ResponseModel(new EzwayPageState
                        {
                            IsLoggedIn = true,
                            QueryCaptchaState = await FetchQueryCaptchaStateAsync()
                        });
                    }
                    catch (EzwaySessionExpiredException)
                    {
                        ClearAuthenticatedSession();
                    }
                }

                // 尚未登入時先準備登入驗證碼畫面所需資料。
                return new ResponseModel(new EzwayPageState
                {
                    IsLoggedIn = false,
                    LoginCaptchaState = await RefreshLoginCaptchaStateAsync(false)
                });
            }
            catch (Exception ex)
            {
                return new ResponseModel($"Ezway 初始化失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 刷新登入驗證碼。
        /// </summary>
        public async Task<ResponseModel> RefreshLoginCaptchaAsync()
        {
            try
            {
                return new ResponseModel(await RefreshLoginCaptchaStateAsync(true));
            }
            catch (Exception ex)
            {
                return new ResponseModel($"刷新登入驗證碼失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 清除目前 Ezway 登入 session，讓畫面可重新登入。
        /// </summary>
        public async Task<ResponseModel> LogoutAsync()
        {
            try
            {
                ClearAuthenticatedSession();
                ClearAnonymousToken();

                EzwayCaptchaState loginCaptchaState;
                try
                {
                    loginCaptchaState = await RefreshLoginCaptchaStateAsync(false);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Ezway 登出後刷新登入驗證碼失敗");
                    loginCaptchaState = new EzwayCaptchaState();
                }

                return new ResponseModel(new EzwayPageState
                {
                    IsLoggedIn = false,
                    LoginCaptchaState = loginCaptchaState
                });
            }
            catch (Exception ex)
            {
                return new ResponseModel($"Ezway 登出失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 執行 Ezway 業者登入。
        /// </summary>
        public async Task<ResponseModel> LoginAsync(EzwayLoginRequest request)
        {
            try
            {
                // 先完成前端必填欄位與驗證碼檢查，避免送出無效登入請求。
                if (request == null)
                {
                    return new ResponseModel("登入資料不可為空");
                }

                if (string.IsNullOrWhiteSpace(request.CompanyId))
                {
                    return new ResponseModel("請輸入統一編號");
                }

                if (string.IsNullOrWhiteSpace(request.Account))
                {
                    return new ResponseModel("請輸入帳號");
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return new ResponseModel("請輸入密碼");
                }

                if (request.CaptchaRequired)
                {
                    if (string.IsNullOrWhiteSpace(request.CaptchaCode))
                    {
                        return new ResponseModel("登入驗證碼已失效，請重新刷新驗證碼");
                    }

                    if (string.IsNullOrWhiteSpace(request.Captcha))
                    {
                        return new ResponseModel("請輸入登入驗證碼");
                    }
                }

                // 登入前先以匿名 token 檢查是否有待同意的服務條款內容。
                string anonymousToken = await EnsureAnonymousTokenAsync();
                string termsHtml = await GetPendingTermsHtmlAsync(anonymousToken, request);

                if (!string.IsNullOrWhiteSpace(termsHtml) && !request.TermsAccepted)
                {
                    return new ResponseModel(new EzwayLoginResult
                    {
                        RequiresTermsAgreement = true,
                        TermsHtml = termsHtml
                    });
                }

                if (!string.IsNullOrWhiteSpace(termsHtml) && request.TermsAccepted)
                {
                    await AgreeTermsAsync(anonymousToken, request);
                }

                // 完成條款確認後再送正式登入，成功後保存 JWT 與業者識別資訊。
                using (var httpClient = CreateHttpClient(anonymousToken))
                using (var requestMessage = CreateJsonRequest(HttpMethod.Post, LoginUrl, BuildLoginPayload(request)))
                {
                    var apiResponse = await SendAsync<LoginData>(httpClient, requestMessage);
                    if (!IsApiSuccess(apiResponse) || apiResponse.Data == null || string.IsNullOrWhiteSpace(apiResponse.Data.Token) || string.IsNullOrWhiteSpace(apiResponse.Data.UserId))
                    {
                        return new ResponseModel(apiResponse?.Msg ?? "Ezway 登入失敗");
                    }

                    StoreAuthenticatedSession(
                        apiResponse.Data.Token,
                        apiResponse.Data.UserId,
                        !string.IsNullOrWhiteSpace(apiResponse.Data.IdNo)
                            ? apiResponse.Data.IdNo.Trim()
                            : !string.IsNullOrWhiteSpace(apiResponse.Data.BrokerBan)
                                ? apiResponse.Data.BrokerBan.Trim()
                                : request.CompanyId.Trim());

                    ClearAnonymousToken();

                    return new ResponseModel(new EzwayLoginResult
                    {
                        IsLoggedIn = true
                    });
                }
            }
            catch (EzwaySessionExpiredException)
            {
                ClearAnonymousToken();
                return new ResponseModel("Ezway 匿名登入資訊已失效，請重新刷新驗證碼後再試");
            }
            catch (Exception ex)
            {
                return new ResponseModel($"Ezway 登入失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 取得查詢階段的驗證碼狀態。
        /// </summary>
        public async Task<ResponseModel> GetQueryCaptchaStateAsync()
        {
            try
            {
                return new ResponseModel(await FetchQueryCaptchaStateAsync());
            }
            catch (EzwaySessionExpiredException)
            {
                ClearAuthenticatedSession();
                return new ResponseModel("Ezway 登入已過期，請重新登入");
            }
            catch (Exception ex)
            {
                return new ResponseModel($"取得查詢驗證碼失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 執行單筆分提單查詢。
        /// </summary>
        public async Task<ResponseModel> QuerySingleAsync(EzwayQueryRequest request)
        {
            try
            {
                List<string> hawbNumbers = ValidateQueryRequest(request);
                ValidateSingleQueryCount(hawbNumbers);

                ValidateQuerySession();
                ValidateQueryCaptcha(request);

                List<EzwayQueryResult> results = new List<EzwayQueryResult>();

                using (var httpClient = CreateHttpClient(GetStoredJwtToken()))
                {
                    foreach (string hawbNumber in hawbNumbers)
                    {
                        using (var requestMessage = CreateJsonRequest(HttpMethod.Post, SingleQueryUrl, BuildSingleQueryPayload(hawbNumber, request)))
                        {
                            var apiResponse = await SendAsync<string>(httpClient, requestMessage);
                            if (!IsApiSuccess(apiResponse))
                            {
                                if (IsNoDataResponse(apiResponse))
                                {
                                    results.Add(CreateNoDataResult(hawbNumber, apiResponse?.Msg));
                                    continue;
                                }

                                return new ResponseModel(apiResponse?.Msg ?? "Ezway 查詢失敗");
                            }

                            List<EzwayQueryResult> queryResults = new List<EzwayQueryResult>();
                            if (!string.IsNullOrWhiteSpace(apiResponse.Data))
                            {
                                string decryptedJson = DecryptSingleQuery(apiResponse.Data);
                                queryResults = DeserializeJson<List<EzwayQueryResult>>(decryptedJson) ?? new List<EzwayQueryResult>();
                            }

                            if (queryResults.Count == 0)
                            {
                                results.Add(CreateNoDataResult(hawbNumber));
                                continue;
                            }

                            results.AddRange(queryResults);
                        }
                    }
                }

                return new ResponseModel(new EzwayQueryResponse
                {
                    Results = NormalizeQueryResults(EnsureRequestedHawbResults(hawbNumbers, results)),
                    QueryCaptchaState = await FetchQueryCaptchaStateAsync()
                });
            }
            catch (EzwaySessionExpiredException)
            {
                ClearAuthenticatedSession();
                return new ResponseModel("Ezway 登入已過期，請重新登入");
            }
            catch (InvalidOperationException ex)
            {
                return new ResponseModel(ex.Message);
            }
            catch (Exception ex)
            {
                return new ResponseModel($"Ezway 查詢失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 執行整批 Excel 查詢。
        /// </summary>
        public async Task<ResponseModel> QueryBatchAsync(EzwayQueryRequest request)
        {
            try
            {
                List<string> hawbNumbers = ValidateQueryRequest(request);

                ValidateQuerySession();
                ValidateQueryCaptcha(request);

                bool isX4Query = IsX4QueryApi(request);
                List<EzwayQueryResult> results = new List<EzwayQueryResult>();
                List<List<string>> batchedHawbNumbers = SplitHawbNumbers(hawbNumbers, 500);

                using (var httpClient = CreateHttpClient(GetStoredJwtToken()))
                {
                    for (int batchIndex = 0; batchIndex < batchedHawbNumbers.Count; batchIndex++)
                    {
                        List<string> batchItems = batchedHawbNumbers[batchIndex];
                        byte[] fileBytes = BuildBatchExcelBytes(batchItems);

                        using (var multipartContent = CreateBatchMultipartContent(fileBytes, isX4Query, batchIndex + 1))
                        using (var requestMessage = CreateMultipartRequest(BatchQueryUrl, multipartContent))
                        {
                            var apiResponse = await SendAsync<List<EzwayQueryResult>>(httpClient, requestMessage);
                            if (!IsApiSuccess(apiResponse))
                            {
                                return new ResponseModel($"第 {batchIndex + 1} 批整批查詢失敗：{apiResponse?.Msg ?? "Ezway 整批查詢失敗"}");
                            }

                            results.AddRange(apiResponse.Data ?? new List<EzwayQueryResult>());
                        }

                        if (batchIndex < batchedHawbNumbers.Count - 1)
                        {
                            await Task.Delay(2000);
                        }
                    }
                }

                return new ResponseModel(new EzwayQueryResponse
                {
                    Results = NormalizeQueryResults(EnsureRequestedHawbResults(hawbNumbers, results)),
                    QueryCaptchaState = await FetchQueryCaptchaStateAsync()
                });
            }
            catch (EzwaySessionExpiredException)
            {
                ClearAuthenticatedSession();
                return new ResponseModel("Ezway 登入已過期，請重新登入");
            }
            catch (Exception ex)
            {
                return new ResponseModel($"Ezway 整批查詢失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 將查詢結果匯出為 Excel。
        /// </summary>
        public async Task<XSSFWorkbook> ExportExcelAsync(EzwayExportRequest request)
        {
            List<EzwayQueryResult> exportResults = await ResolveExportResultsAsync(request);
            if (exportResults.Count == 0)
            {
                throw new InvalidOperationException("查無結果可匯出");
            }

            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Ezway查詢結果");

            ICellStyle headerStyle = NpoiStyle.CreateHeaderStyle(workbook, 11, true);
            ICellStyle textStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Left);
            ICellStyle centerStyle = NpoiStyle.CreateDataStyle(workbook, HorizontalAlignment.Center);

            var headerRow = sheet.CreateRow(0);
            NpoiCell.CreateCell(headerRow, 0, "預報關日期", headerStyle);
            NpoiCell.CreateCell(headerRow, 1, "報單號碼", headerStyle);
            NpoiCell.CreateCell(headerRow, 2, "主提單號碼", headerStyle);
            NpoiCell.CreateCell(headerRow, 3, "分提單號碼", headerStyle);
            NpoiCell.CreateCell(headerRow, 4, "電話號碼", headerStyle);
            NpoiCell.CreateCell(headerRow, 5, "證件號碼", headerStyle);
            NpoiCell.CreateCell(headerRow, 6, "實名委任日期", headerStyle);
            NpoiCell.CreateCell(headerRow, 7, "認證結果", headerStyle);
            NpoiCell.CreateCell(headerRow, 8, "核准文號", headerStyle);
            NpoiCell.CreateCell(headerRow, 9, "海關回覆結果", headerStyle);
            NpoiCell.CreateCell(headerRow, 10, "海關回覆日期", headerStyle);
            NpoiCell.CreateCell(headerRow, 11, "阻擋原因", headerStyle);

            for (int index = 0; index < exportResults.Count; index++)
            {
                EzwayQueryResult item = exportResults[index];
                var row = sheet.CreateRow(index + 1);

                NpoiCell.CreateCell(row, 0, item.ImportDate ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, 1, item.DeclNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, 2, item.MawbNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, 3, item.HawbNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, 4, item.TelNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, 5, item.IdNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, 6, BuildReplyDateTime(item), centerStyle);
                NpoiCell.CreateCell(row, 7, item.IsReply ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, 8, item.AuthorizeDocNo ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, 9, item.AuthorizeReply ?? string.Empty, textStyle);
                NpoiCell.CreateCell(row, 10, item.AuthorizeDatm ?? string.Empty, centerStyle);
                NpoiCell.CreateCell(row, 11, item.BlockReason ?? string.Empty, textStyle);
            }

            for (int columnIndex = 0; columnIndex <= 11; columnIndex++)
            {
                sheet.AutoSizeColumn(columnIndex);
                if (sheet.GetColumnWidth(columnIndex) < 4000)
                {
                    sheet.SetColumnWidth(columnIndex, 4000);
                }
            }

            return workbook;
        }

        /// <summary>
        /// 取得匯出 Excel 所需的查詢結果，必要時會依目前條件先執行查詢。
        /// </summary>
        private async Task<List<EzwayQueryResult>> ResolveExportResultsAsync(EzwayExportRequest request)
        {
            if (request?.Results != null && request.Results.Count > 0)
            {
                return NormalizeQueryResults(request.Results);
            }

            if (request?.QueryRequest == null)
            {
                throw new InvalidOperationException("請先輸入分提單號後再匯出 Excel");
            }

            ResponseModel queryResult = string.Equals(ResolveManual(request.QueryRequest.Manual, "Y"), "N", StringComparison.OrdinalIgnoreCase)
                ? await QueryBatchAsync(request.QueryRequest)
                : await QuerySingleAsync(request.QueryRequest);

            if (!queryResult.IsSuccess)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(queryResult.msg) ? "匯出前查詢失敗" : queryResult.msg);
            }

            EzwayQueryResponse queryResponse = queryResult.ReturnObject as EzwayQueryResponse;
            if (queryResponse?.Results == null || queryResponse.Results.Count == 0)
            {
                throw new InvalidOperationException("查無結果可匯出");
            }

            return queryResponse.Results;
        }

        /// <summary>
        /// 取得登入階段的驗證碼狀態，必要時會重新申請匿名 token。
        /// </summary>
        private async Task<EzwayCaptchaState> RefreshLoginCaptchaStateAsync(bool forceRefreshAnonymousToken)
        {
            string anonymousToken = forceRefreshAnonymousToken ? null : GetStoredAnonymousToken();
            if (string.IsNullOrWhiteSpace(anonymousToken))
            {
                anonymousToken = await RequestAnonymousTokenAsync();
            }

            using (var httpClient = CreateHttpClient(anonymousToken))
            using (var requestMessage = CreateJsonRequest(HttpMethod.Get, LoginCaptchaUrl))
            {
                EzwayCaptchaState captchaState = await ReadCaptchaStateAsync(httpClient, requestMessage);
                StoreAnonymousToken(anonymousToken);
                return captchaState;
            }
        }

        /// <summary>
        /// 取得可用的匿名 token。
        /// </summary>
        private async Task<string> EnsureAnonymousTokenAsync()
        {
            string anonymousToken = GetStoredAnonymousToken();
            if (!string.IsNullOrWhiteSpace(anonymousToken))
            {
                return anonymousToken;
            }

            anonymousToken = await RequestAnonymousTokenAsync();
            StoreAnonymousToken(anonymousToken);
            return anonymousToken;
        }

        /// <summary>
        /// 向 ECCS 申請匿名 token。
        /// </summary>
        private async Task<string> RequestAnonymousTokenAsync()
        {
            using (var httpClient = CreateHttpClient())
            using (var requestMessage = CreateJsonRequest(HttpMethod.Post, AnonymousTokenUrl, new
            {
                authId = string.Empty,
                lang = "TW"
            }))
            {
                var apiResponse = await SendAsync<TokenData>(httpClient, requestMessage);
                if (!IsApiSuccess(apiResponse) || apiResponse.Data == null || string.IsNullOrWhiteSpace(apiResponse.Data.Token))
                {
                    throw new InvalidOperationException(apiResponse?.Msg ?? "取得 Ezway 匿名 token 失敗");
                }

                return NormalizeBearerToken(apiResponse.Data.Token);
            }
        }

        /// <summary>
        /// 取得待同意的服務條款 HTML。
        /// </summary>
        private async Task<string> GetPendingTermsHtmlAsync(string anonymousToken, EzwayLoginRequest request)
        {
            using (var httpClient = CreateHttpClient(anonymousToken))
            using (var requestMessage = CreateJsonRequest(HttpMethod.Post, TermsPreviewUrl, BuildTermsPreviewPayload(request)))
            {
                var apiResponse = await SendAsync<List<AnnouncementItem>>(httpClient, requestMessage);
                if (!IsApiSuccess(apiResponse) || apiResponse.Data == null || apiResponse.Data.Count == 0)
                {
                    return string.Empty;
                }

                return apiResponse.Data
                    .Select(item => item?.Context)
                    .FirstOrDefault(context => !string.IsNullOrWhiteSpace(context)) ?? string.Empty;
            }
        }

        /// <summary>
        /// 同意 Ezway 服務條款。
        /// </summary>
        private async Task AgreeTermsAsync(string anonymousToken, EzwayLoginRequest request)
        {
            using (var httpClient = CreateHttpClient(anonymousToken))
            using (var requestMessage = CreateJsonRequest(HttpMethod.Post, TermsAgreeUrl, BuildTermsAgreementPayload(request)))
            {
                var apiResponse = await SendAsync<object>(httpClient, requestMessage);
                if (!IsApiSuccess(apiResponse))
                {
                    throw new InvalidOperationException(apiResponse?.Msg ?? "服務條款確認失敗");
                }
            }
        }

        /// <summary>
        /// 取得查詢驗證碼狀態。
        /// </summary>
        private async Task<EzwayCaptchaState> FetchQueryCaptchaStateAsync()
        {
            ValidateQuerySession();

            string url = QuerySettingUrl + "?userId=" + HttpUtility.UrlEncode(GetStoredUserId());

            using (var httpClient = CreateHttpClient(GetStoredJwtToken()))
            using (var requestMessage = CreateJsonRequest(HttpMethod.Get, url))
            {
                return await ReadCaptchaStateAsync(httpClient, requestMessage);
            }
        }

        /// <summary>
        /// 解析驗證碼 API 回傳內容。
        /// </summary>
        private async Task<EzwayCaptchaState> ReadCaptchaStateAsync(HttpClient httpClient, HttpRequestMessage requestMessage)
        {
            var apiResponse = await SendAsync<CaptchaData>(httpClient, requestMessage);
            if (apiResponse == null)
            {
                throw new InvalidOperationException("無法解析 Ezway 驗證碼回應");
            }

            if (!IsApiSuccess(apiResponse) || apiResponse.Data == null)
            {
                return new EzwayCaptchaState();
            }

            return new EzwayCaptchaState
            {
                CaptchaRequired = true,
                CaptchaCode = apiResponse.Data.Code,
                CaptchaImageBase64 = string.IsNullOrWhiteSpace(apiResponse.Data.Image)
                    ? string.Empty
                    : "data:image/png;base64," + apiResponse.Data.Image
            };
        }

        /// <summary>
        /// 確認目前 session 已具備查詢所需登入資訊。
        /// </summary>
        private void ValidateQuerySession()
        {
            if (!HasAuthenticatedSession())
            {
                throw new InvalidOperationException("Ezway 尚未登入，請先完成登入");
            }

            if (string.IsNullOrWhiteSpace(GetStoredBrokerBan()))
            {
                throw new InvalidOperationException("Ezway 業者資訊不存在，請重新登入");
            }
        }

        /// <summary>
        /// 驗證查詢驗證碼欄位是否完整。
        /// </summary>
        private static void ValidateQueryCaptcha(EzwayQueryRequest request)
        {
            if (request != null && request.QueryCaptchaRequired)
            {
                if (string.IsNullOrWhiteSpace(request.QueryCaptchaCode))
                {
                    throw new InvalidOperationException("查詢驗證碼已失效，請重新刷新驗證碼");
                }

                if (string.IsNullOrWhiteSpace(request.QueryCaptcha))
                {
                    throw new InvalidOperationException("請輸入查詢驗證碼");
                }
            }
        }

        /// <summary>
        /// 建立正式登入 payload。
        /// </summary>
        private object BuildLoginPayload(EzwayLoginRequest request)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "lang", "TW" },
                { "idNo", request.CompanyId.Trim() },
                { "userId", request.Account.Trim() },
                { "userPwd", request.Password },
                { "userType", "CUSTOMER" },
                { "personCheck", "Y" }
            };

            if (request.CaptchaRequired)
            {
                payload["code"] = request.CaptchaCode.Trim();
                payload["captcha"] = request.Captcha.Trim();
                payload["result"] = "Y";
            }

            return payload;
        }

        /// <summary>
        /// 建立條款預覽 payload。
        /// </summary>
        private object BuildTermsPreviewPayload(EzwayLoginRequest request)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "userId", request.Account.Trim() },
                { "idNo", request.CompanyId.Trim() },
                { "userPwd", request.Password },
                { "type", "C" },
                { "lang", "TW" }
            };

            if (request.CaptchaRequired)
            {
                payload["code"] = request.CaptchaCode?.Trim();
                payload["captcha"] = request.Captcha?.Trim();
            }

            return payload;
        }

        /// <summary>
        /// 建立條款同意 payload。
        /// </summary>
        private object BuildTermsAgreementPayload(EzwayLoginRequest request)
        {
            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                { "userId", request.Account.Trim() },
                { "idNo", request.CompanyId.Trim() },
                { "userPwd", request.Password }
            };

            if (request.CaptchaRequired)
            {
                payload["code"] = request.CaptchaCode?.Trim();
                payload["captcha"] = request.Captcha?.Trim();
            }

            return payload;
        }

        /// <summary>
        /// 建立單筆查詢 payload。
        /// </summary>
        private object BuildSingleQueryPayload(string hawbNumber, EzwayQueryRequest request)
        {
            return IsX4QueryApi(request)
                ? BuildX4SingleQueryPayload(hawbNumber)
                : BuildSimpleSingleQueryPayload(hawbNumber);
        }

        /// <summary>
        /// 建立「預先委任確認查詢(X4)」單筆查詢 payload。
        /// </summary>
        private Dictionary<string, object> BuildX4SingleQueryPayload(string hawbNumber)
        {
            return new Dictionary<string, object>
            {
                { "authorizeStatus", "A" },
                { "brokerBan", GetStoredBrokerBan() },
                { "declType", "G1" },
                { "hawbNo", hawbNumber },
                { "lang", "TW" },
                { "manual", "Y" },
                { "status", "A" },
                { "userId", GetStoredUserId() }
            };
        }

        /// <summary>
        /// 建立「預先委任確認查詢(簡易)」單筆查詢 payload。
        /// </summary>
        private Dictionary<string, object> BuildSimpleSingleQueryPayload(string hawbNumber)
        {
            return new Dictionary<string, object>
            {
                { "authorizeStatus", "A" },
                { "brokerBan", GetStoredBrokerBan() },
                { "declType", "TX" },
                { "groupUserId", "全部" },
                { "hawbNo", hawbNumber },
                { "lang", "TW" },
                { "manual", "Y" },
                { "status", "A" },
                { "userId", GetStoredUserId() }
            };
        }

        /// <summary>
        /// 驗證單筆查詢條件。
        /// </summary>
        private static List<string> ValidateQueryRequest(EzwayQueryRequest request)
        {
            if (request == null)
            {
                throw new InvalidOperationException("查詢資料不可為空");
            }

            List<string> hawbNumbers = ExtractHawbNumbers(request.HawbNo);
            if (hawbNumbers.Count == 0)
            {
                throw new InvalidOperationException("請輸入分提單號");
            }

            if (hawbNumbers.Any(hawbNumber => hawbNumber.Length > 35))
            {
                throw new InvalidOperationException("分提單號碼最長 35 碼");
            }

            return hawbNumbers;
        }

        /// <summary>
        /// 驗證單筆查詢筆數限制。
        /// </summary>
        private static void ValidateSingleQueryCount(List<string> hawbNumbers)
        {
            if ((hawbNumbers?.Count ?? 0) > SingleQueryMaxCount)
            {
                throw new InvalidOperationException("查詢超過10筆，請使用整批查詢");
            }
        }

        /// <summary>
        /// 去除查詢欄位前後空白。
        /// </summary>
        private static string TrimValue(string value)
        {
            return value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 將多行分提單輸入轉成查詢清單。
        /// </summary>
        private static List<string> ExtractHawbNumbers(string hawbNumbersText)
        {
            return (hawbNumbersText ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(TrimValue)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 將分提單清單切成每批指定筆數。
        /// </summary>
        private static List<List<string>> SplitHawbNumbers(List<string> hawbNumbers, int batchSize)
        {
            List<List<string>> batches = new List<List<string>>();
            for (int index = 0; index < hawbNumbers.Count; index += batchSize)
            {
                batches.Add(hawbNumbers.Skip(index).Take(batchSize).ToList());
            }

            return batches;
        }

        /// <summary>
        /// 依分提單清單建立整批查詢所需的 Excel 檔案。
        /// </summary>
        private static byte[] BuildBatchExcelBytes(List<string> hawbNumbers)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("分提單號");

            sheet.CreateRow(0).CreateCell(0).SetCellValue("分提單號");
            for (int index = 0; index < hawbNumbers.Count; index++)
            {
                sheet.CreateRow(index + 1).CreateCell(0).SetCellValue(hawbNumbers[index]);
            }

            using (var memoryStream = new MemoryStream())
            {
                workbook.Write(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// 建立整批查詢 multipart/form-data 內容。
        /// </summary>
        private MultipartFormDataContent CreateBatchMultipartContent(byte[] fileBytes, bool isX4Query, int batchNumber)
        {
            return isX4Query
                ? CreateX4BatchMultipartContent(fileBytes, batchNumber)
                : CreateSimpleBatchMultipartContent(fileBytes, batchNumber);
        }

        /// <summary>
        /// 建立「預先委任確認查詢(X4)」整批查詢 multipart/form-data 內容。
        /// </summary>
        private MultipartFormDataContent CreateX4BatchMultipartContent(byte[] fileBytes, int batchNumber)
        {
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent("N"), "manual");
            multipartContent.Add(CreateBatchFileContent(fileBytes), "file", $"EzwayBatch_{batchNumber:000}.xlsx");
            multipartContent.Add(new StringContent(GetStoredUserId()), "userId");
            multipartContent.Add(new StringContent("G1"), "declType");
            multipartContent.Add(new StringContent(GetStoredBrokerBan()), "brokerBan");
            multipartContent.Add(new StringContent("A"), "status");
            multipartContent.Add(new StringContent("TW"), "lang");
            multipartContent.Add(new StringContent("A"), "authorizeStatus");
            return multipartContent;
        }

        /// <summary>
        /// 建立「預先委任確認查詢(簡易)」整批查詢 multipart/form-data 內容。
        /// </summary>
        private MultipartFormDataContent CreateSimpleBatchMultipartContent(byte[] fileBytes, int batchNumber)
        {
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent("N"), "manual");
            multipartContent.Add(CreateBatchFileContent(fileBytes), "file", $"EzwayBatch_{batchNumber:000}.xlsx");
            multipartContent.Add(new StringContent(GetStoredUserId()), "userId");
            multipartContent.Add(new StringContent("TX"), "declType");
            multipartContent.Add(new StringContent(GetStoredBrokerBan()), "brokerBan");
            multipartContent.Add(new StringContent("A"), "status");
            multipartContent.Add(new StringContent("A"), "authorizeStatus");
            multipartContent.Add(new StringContent("TW"), "lang");
            multipartContent.Add(new StringContent("全部"), "groupUserId");
            return multipartContent;
        }

        /// <summary>
        /// 建立整批查詢用的 Excel 檔案內容。
        /// </summary>
        private static ByteArrayContent CreateBatchFileContent(byte[] fileBytes)
        {
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetExcelMediaType(".xlsx"));
            return fileContent;
        }

        /// <summary>
        /// 判斷是否使用 X4 查詢規格。
        /// </summary>
        private static bool IsX4QueryApi(EzwayQueryRequest request)
        {
            return string.Equals(request?.QueryApiType, "X4", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 將查詢方式正規化為 Ezway API 可接受值。
        /// </summary>
        private static string ResolveManual(string manual, string defaultValue)
        {
            return string.Equals(manual, "Y", StringComparison.OrdinalIgnoreCase)
                || string.Equals(manual, "N", StringComparison.OrdinalIgnoreCase)
                ? manual.ToUpperInvariant()
                : defaultValue;
        }

        /// <summary>
        /// 將委任狀態正規化為 Ezway API 可接受值。
        /// </summary>
        private static string ResolveStatus(string status)
        {
            return ResolveQueryOption(status, "A", "A", "Y", "N", "W");
        }

        /// <summary>
        /// 將海關回覆狀態正規化為 Ezway API 可接受值。
        /// </summary>
        private static string ResolveAuthorizeStatus(string authorizeStatus)
        {
            return ResolveQueryOption(authorizeStatus, "A", "A", "Y", "N");
        }

        /// <summary>
        /// 共用查詢選項正規化處理。
        /// </summary>
        private static string ResolveQueryOption(string value, string defaultValue, params string[] allowedValues)
        {
            string normalizedValue = value?.Trim().ToUpperInvariant();
            return allowedValues.Contains(normalizedValue) ? normalizedValue : defaultValue;
        }

        /// <summary>
        /// 將 Ezway 查詢結果中的代碼與日期欄位轉成畫面可直接顯示的格式。
        /// </summary>
        private static List<EzwayQueryResult> NormalizeQueryResults(List<EzwayQueryResult> results)
        {
            if (results == null)
            {
                return new List<EzwayQueryResult>();
            }

            foreach (EzwayQueryResult item in results)
            {
                NormalizeQueryResult(item);
            }

            return results;
        }

        /// <summary>
        /// 正規化單筆查詢結果欄位。
        /// </summary>
        private static void NormalizeQueryResult(EzwayQueryResult item)
        {
            if (item == null)
            {
                return;
            }

            item.IsReply = TranslateCode(item.IsReply, IsReplyMappings);
            item.AuthorizeReply = TranslateCode(item.AuthorizeReply, AuthorizeReplyMappings);
            item.ImportDate = FormatApiDate(item.ImportDate);
            item.ReplyDate = FormatApiDateTime(item.ReplyDate, item.ReplyTime);
            item.ReplyTime = string.Empty;
            item.AuthorizeDatm = FormatApiDateTime(item.AuthorizeDatm);
        }

        /// <summary>
        /// 依輸入的分提單順序補齊查無資料的結果列。
        /// </summary>
        private static List<EzwayQueryResult> EnsureRequestedHawbResults(List<string> hawbNumbers, List<EzwayQueryResult> results)
        {
            List<EzwayQueryResult> sourceResults = (results ?? new List<EzwayQueryResult>())
                .Where(item => item != null)
                .ToList();

            Dictionary<string, List<EzwayQueryResult>> resultLookup = sourceResults
                .GroupBy(item => TrimValue(item.HawbNo), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            HashSet<EzwayQueryResult> appendedResults = new HashSet<EzwayQueryResult>();
            List<EzwayQueryResult> orderedResults = new List<EzwayQueryResult>();

            foreach (string hawbNumber in hawbNumbers ?? new List<string>())
            {
                if (resultLookup.TryGetValue(hawbNumber, out List<EzwayQueryResult> matchedResults) && matchedResults.Count > 0)
                {
                    foreach (EzwayQueryResult item in matchedResults)
                    {
                        if (string.IsNullOrWhiteSpace(item.HawbNo))
                        {
                            item.HawbNo = hawbNumber;
                        }

                        orderedResults.Add(item);
                        appendedResults.Add(item);
                    }

                    continue;
                }

                orderedResults.Add(CreateNoDataResult(hawbNumber));
            }

            foreach (EzwayQueryResult item in sourceResults)
            {
                if (!appendedResults.Contains(item))
                {
                    orderedResults.Add(item);
                }
            }

            return orderedResults;
        }

        /// <summary>
        /// 建立查無資料時的預設結果列。
        /// </summary>
        private static EzwayQueryResult CreateNoDataResult(string hawbNumber, string message = null)
        {
            return new EzwayQueryResult
            {
                HawbNo = hawbNumber,
                IsReply = "查無資料",
                BlockReason = string.IsNullOrWhiteSpace(message) ? NoDataMessage : message.Trim()
            };
        }

        /// <summary>
        /// 將 API 回傳代碼轉換為中文說明。
        /// </summary>
        private static string TranslateCode(string value, IReadOnlyDictionary<string, string> mappings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalizedValue = value.Trim();
            return mappings.TryGetValue(normalizedValue, out string translatedValue)
                ? translatedValue
                : normalizedValue;
        }

        /// <summary>
        /// 將 API 日期欄位格式化為 yyyy/MM/dd。
        /// </summary>
        private static string FormatApiDate(string dateValue)
        {
            string normalizedDateValue = NormalizeDateTimeSource(dateValue);
            if (string.IsNullOrWhiteSpace(normalizedDateValue))
            {
                return string.Empty;
            }

            if (TryParseApiDateTime(normalizedDateValue, null, out DateTime parsedDate))
            {
                return parsedDate.ToString("yyyy/MM/dd");
            }

            return normalizedDateValue;
        }

        /// <summary>
        /// 將 API 日期欄位格式化為 yyyy/MM/dd HH:mm:ss。
        /// </summary>
        private static string FormatApiDateTime(string dateValue, string timeValue = null)
        {
            string normalizedDateValue = NormalizeDateTimeSource(dateValue);
            string normalizedTimeValue = NormalizeDateTimeSource(timeValue);

            if (string.IsNullOrWhiteSpace(normalizedDateValue) && string.IsNullOrWhiteSpace(normalizedTimeValue))
            {
                return string.Empty;
            }

            if (TryParseApiDateTime(normalizedDateValue, normalizedTimeValue, out DateTime parsedDateTime))
            {
                return parsedDateTime.ToString("yyyy/MM/dd HH:mm:ss");
            }

            if (TryParseApiDateTime(normalizedDateValue, null, out parsedDateTime))
            {
                return parsedDateTime.ToString("yyyy/MM/dd HH:mm:ss");
            }

            if (TryParseApiDateTime(normalizedTimeValue, null, out parsedDateTime))
            {
                return parsedDateTime.ToString("yyyy/MM/dd HH:mm:ss");
            }

            return string.IsNullOrWhiteSpace(normalizedTimeValue)
                ? normalizedDateValue
                : string.Concat(normalizedDateValue, " ", normalizedTimeValue).Trim();
        }

        /// <summary>
        /// 移除日期來源中的空白與常見分隔符，方便後續統一解析。
        /// </summary>
        private static string NormalizeDateTimeSource(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// 嘗試解析 Ezway 日期與時間欄位。
        /// </summary>
        private static bool TryParseApiDateTime(string dateValue, string timeValue, out DateTime parsedDateTime)
        {
            string combinedValue = BuildCombinedDateTimeValue(dateValue, timeValue);
            if (DateTime.TryParseExact(
                combinedValue,
                AcceptedDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDateTime))
            {
                return true;
            }

            return DateTime.TryParse(combinedValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime)
                || DateTime.TryParse(combinedValue, CultureInfo.GetCultureInfo("zh-TW"), DateTimeStyles.None, out parsedDateTime);
        }

        /// <summary>
        /// 組合日期與時間原始字串。
        /// </summary>
        private static string BuildCombinedDateTimeValue(string dateValue, string timeValue)
        {
            if (string.IsNullOrWhiteSpace(dateValue))
            {
                return timeValue ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(timeValue))
            {
                return dateValue;
            }

            if (dateValue.IndexOf(':') >= 0 || dateValue.IndexOf('T') >= 0)
            {
                return dateValue;
            }

            return string.Concat(dateValue, timeValue);
        }

        /// <summary>
        /// 建立 HTTP Client，並在需要時加入 Bearer token。
        /// </summary>
        private HttpClient CreateHttpClient(string bearerToken = null)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-TW,zh;q=0.9,en-US;q=0.8,en;q=0.7");

            string normalizedToken = NormalizeBearerToken(bearerToken);
            if (!string.IsNullOrWhiteSpace(normalizedToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", normalizedToken);
            }

            return httpClient;
        }

        /// <summary>
        /// 建立 JSON 格式 HTTP 請求。
        /// </summary>
        private HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, object payload = null)
        {
            var request = new HttpRequestMessage(method, url);
            ApplySignHeaders(request);

            if (payload != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            }

            return request;
        }

        /// <summary>
        /// 建立 multipart/form-data HTTP 請求。
        /// </summary>
        private HttpRequestMessage CreateMultipartRequest(string url, MultipartFormDataContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            ApplySignHeaders(request);
            return request;
        }

        /// <summary>
        /// 傳送 HTTP 請求並記錄安全化 request/response log。
        /// </summary>
        private async Task<EccsApiResponse<T>> SendAsync<T>(HttpClient httpClient, HttpRequestMessage request)
        {
            await LogApiRequestAsync(request);

            using (var response = await httpClient.SendAsync(request))
            {
                string content = await response.Content.ReadAsStringAsync();
                LogApiResponse(request, response, content);

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new EzwaySessionExpiredException();
                }

                EccsApiResponse<T> apiResponse = DeserializeJson<EccsApiResponse<T>>(content);
                if (apiResponse != null)
                {
                    if (string.IsNullOrWhiteSpace(apiResponse.Msg) && !string.IsNullOrWhiteSpace(apiResponse.ReturnMsg))
                    {
                        apiResponse.Msg = apiResponse.ReturnMsg;
                    }

                    if (string.IsNullOrWhiteSpace(apiResponse.Msg) && !response.IsSuccessStatusCode)
                    {
                        apiResponse.Msg = response.ReasonPhrase;
                    }

                    return apiResponse;
                }

                return new EccsApiResponse<T>
                {
                    Status = "N",
                    Msg = string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase : content
                };
            }
        }

        /// <summary>
        /// 記錄送往 ECCS 的 request log，並遮蔽敏感資訊。
        /// </summary>
        private async Task LogApiRequestAsync(HttpRequestMessage request)
        {
            try
            {
                string apiName = ResolveApiLogName(request?.RequestUri);
                string headers = SerializeLogValue(BuildRequestHeaders(request));
                string body = await BuildRequestBodyLogAsync(request.Content);
                Logger.Info($"Ezway API 請求記錄：API={apiName}, 方法={request.Method}, 網址={request.RequestUri}, 標頭={headers}, 內容={body}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Ezway API 請求記錄略過：{request?.Method} {request?.RequestUri}");
            }
        }

        /// <summary>
        /// 記錄 ECCS 回傳的 response log，並遮蔽敏感資訊。
        /// </summary>
        private void LogApiResponse(HttpRequestMessage request, HttpResponseMessage response, string content)
        {
            try
            {
                string apiName = ResolveApiLogName(request?.RequestUri);
                string sanitizedContent = SanitizeSerializedContent(content);
                Logger.Info($"Ezway API 回應記錄：API={apiName}, 方法={request.Method}, 網址={request.RequestUri}, 狀態={(int)response.StatusCode}, 內容={sanitizedContent}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Ezway API 回應記錄略過：{request?.Method} {request?.RequestUri}");
            }
        }

        /// <summary>
        /// 將 API 網址轉成較容易辨識的 log 名稱。
        /// </summary>
        private static string ResolveApiLogName(Uri requestUri)
        {
            if (requestUri == null)
            {
                return string.Empty;
            }

            string requestUrl = requestUri.GetLeftPart(UriPartial.Path);
            if (requestUrl.StartsWith(QuerySettingUrl, StringComparison.OrdinalIgnoreCase))
            {
                return ApiLogNames[QuerySettingUrl];
            }

            return ApiLogNames.TryGetValue(requestUrl, out string apiName)
                ? apiName
                : requestUrl;
        }

        /// <summary>
        /// 建立可安全寫入 log 的 request header 內容。
        /// </summary>
        private static Dictionary<string, object> BuildRequestHeaders(HttpRequestMessage request)
        {
            Dictionary<string, object> headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (request == null)
            {
                return headers;
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }

            if (request.Content != null)
            {
                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                {
                    headers[header.Key] = string.Join(",", header.Value);
                }
            }

            return headers;
        }

        /// <summary>
        /// 建立可安全寫入 log 的 request body 內容。
        /// </summary>
        private async Task<string> BuildRequestBodyLogAsync(HttpContent content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            if (content is MultipartFormDataContent multipartContent)
            {
                Dictionary<string, object> payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (HttpContent part in multipartContent)
                {
                    string name = part.Headers?.ContentDisposition?.Name?.Trim('"') ?? string.Empty;
                    string filePartName = part.Headers?.ContentDisposition?.FileName?.Trim('"');

                    if (!string.IsNullOrWhiteSpace(filePartName))
                    {
                        payload[name] = new
                        {
                            FileName = Path.GetFileName(filePartName),
                            ContentType = part.Headers?.ContentType?.MediaType,
                            ContentLength = part.Headers?.ContentLength
                        };
                        continue;
                    }

                    payload[name] = await part.ReadAsStringAsync();
                }

                return SerializeLogValue(payload);
            }

            return SanitizeSerializedContent(await content.ReadAsStringAsync());
        }

        /// <summary>
        /// 將物件轉為可安全寫入 log 的 JSON 字串。
        /// </summary>
        private static string SerializeLogValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            JToken token = value as JToken ?? JToken.FromObject(value);
            return TruncateForLog(JsonConvert.SerializeObject(SanitizeLogToken(token), Formatting.None), 2000);
        }

        /// <summary>
        /// 將序列化字串中的敏感欄位遮蔽後回傳。
        /// </summary>
        private static string SanitizeSerializedContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            JToken token = DeserializeJson<JToken>(content);
            return token != null
                ? SerializeLogValue(token)
                : TruncateForLog(content, 2000);
        }

        /// <summary>
        /// 遞迴遮蔽 log 中的敏感欄位與過長資料。
        /// </summary>
        private static JToken SanitizeLogToken(JToken token, string propertyName = null)
        {
            if (token == null)
            {
                return JValue.CreateNull();
            }

            if (token.Type == JTokenType.Object)
            {
                JObject result = new JObject();
                foreach (JProperty property in token.Children<JProperty>())
                {
                    result[property.Name] = SanitizeLogToken(property.Value, property.Name);
                }

                return result;
            }

            if (token.Type == JTokenType.Array)
            {
                JArray source = (JArray)token;
                if (source.Count > MaxLoggedArrayPreviewCount)
                {
                    return new JObject
                    {
                        ["count"] = source.Count,
                        ["preview"] = new JArray(source.Take(MaxLoggedArrayPreviewCount).Select(item => SanitizeLogToken(item, propertyName)))
                    };
                }

                return new JArray(source.Select(item => SanitizeLogToken(item, propertyName)));
            }

            if (token.Type == JTokenType.String)
            {
                return new JValue(SanitizeLogString(propertyName, token.Value<string>()));
            }

            return token.DeepClone();
        }

        /// <summary>
        /// 遮蔽單一字串欄位內容。
        /// </summary>
        private static string SanitizeLogString(string propertyName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (!string.IsNullOrWhiteSpace(propertyName) && FullyMaskedLogFields.Contains(propertyName))
            {
                return MaskSecret(value);
            }

            if (!string.IsNullOrWhiteSpace(propertyName) && PartiallyMaskedLogFields.Contains(propertyName))
            {
                return MaskPartial(value);
            }

            if (string.Equals(propertyName, "data", StringComparison.OrdinalIgnoreCase) && value.Length > MaxLoggedStringLength)
            {
                return $"<omitted length={value.Length}>";
            }

            return TruncateForLog(value, MaxLoggedStringLength);
        }

        /// <summary>
        /// 將完整敏感資訊以固定樣式遮蔽。
        /// </summary>
        private static string MaskSecret(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : $"***({value.Length})";
        }

        /// <summary>
        /// 將身分證號與電話等欄位做部分遮蔽。
        /// </summary>
        private static string MaskPartial(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.Length <= 4)
            {
                return new string('*', value.Length);
            }

            return value.Substring(0, 2)
                + new string('*', Math.Max(1, value.Length - 4))
                + value.Substring(value.Length - 2);
        }

        /// <summary>
        /// 截斷過長字串，避免 log 過度膨脹。
        /// </summary>
        private static string TruncateForLog(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// 套用 Ezway 規格要求的簽章 header。
        /// </summary>
        private static void ApplySignHeaders(HttpRequestMessage request)
        {
            SignHeaders signHeaders = CreateSignHeaders();
            request.Headers.TryAddWithoutValidation("Timestamp", signHeaders.Timestamp);
            request.Headers.TryAddWithoutValidation("Sign", signHeaders.Sign);
        }

        /// <summary>
        /// 產生 Ezway 規格所需簽章。
        /// </summary>
        private static SignHeaders CreateSignHeaders()
        {
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string nonce = CreateNonce(12);
            string digest = ComputeMd5LowerHex(nonce + timestamp + SignSecret);

            return new SignHeaders
            {
                Timestamp = timestamp,
                Sign = nonce + digest
            };
        }

        /// <summary>
        /// 依規格產生簽章 nonce。
        /// </summary>
        private static string CreateNonce(int length)
        {
            char[] result = new char[length];
            byte[] buffer = new byte[length];

            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(buffer);
            }

            for (int index = 0; index < length; index++)
            {
                result[index] = SignChars[buffer[index] % SignChars.Length];
            }

            return new string(result);
        }

        /// <summary>
        /// 產生小寫 MD5 十六進位字串。
        /// </summary>
        private static string ComputeMd5LowerHex(string raw)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        /// <summary>
        /// 解密 ECCS 單筆查詢回傳的 AES-GCM 資料。
        /// </summary>
        private static string DecryptSingleQuery(string encryptedDataBase64)
        {
            if (string.IsNullOrWhiteSpace(encryptedDataBase64))
            {
                return string.Empty;
            }

            byte[] encryptedBytes = Convert.FromBase64String(encryptedDataBase64);
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(QueryDecryptKey), 128, QueryDecryptIv);

            cipher.Init(false, parameters);

            byte[] plaintext = new byte[cipher.GetOutputSize(encryptedBytes.Length)];
            int outputLength = cipher.ProcessBytes(encryptedBytes, 0, encryptedBytes.Length, plaintext, 0);

            try
            {
                outputLength += cipher.DoFinal(plaintext, outputLength);
            }
            catch (InvalidCipherTextException ex)
            {
                throw new InvalidOperationException("Ezway 單筆查詢結果解密失敗", ex);
            }

            return Encoding.UTF8.GetString(plaintext, 0, outputLength);
        }

        /// <summary>
        /// 組合查詢結果中的委任日期與時間。
        /// </summary>
        private static string BuildReplyDateTime(EzwayQueryResult item)
        {
            string replyDate = item?.ReplyDate?.Trim() ?? string.Empty;
            string replyTime = item?.ReplyTime?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(replyDate))
            {
                return replyTime;
            }

            if (string.IsNullOrWhiteSpace(replyTime))
            {
                return replyDate;
            }

            return replyDate + " " + replyTime;
        }

        /// <summary>
        /// 依副檔名回傳 Excel MIME type。
        /// </summary>
        private static string GetExcelMediaType(string extension)
        {
            return string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.ms-excel"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }

        /// <summary>
        /// 將 JSON 字串反序列化為指定型別。
        /// </summary>
        private static T DeserializeJson<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 判斷 ECCS API 是否回傳成功狀態。
        /// </summary>
        private static bool IsApiSuccess<T>(EccsApiResponse<T> response)
        {
            return response != null && string.Equals(response.Status, "Y", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判斷 ECCS 是否回傳查無資料訊息。
        /// </summary>
        private static bool IsNoDataResponse<T>(EccsApiResponse<T> response)
        {
            return !string.IsNullOrWhiteSpace(response?.Msg)
                && response.Msg.IndexOf(NoDataMessage, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 判斷目前是否已有完整登入 session。
        /// </summary>
        private bool HasAuthenticatedSession()
        {
            return !string.IsNullOrWhiteSpace(GetStoredJwtToken())
                && !string.IsNullOrWhiteSpace(GetStoredUserId());
        }

        /// <summary>
        /// 取得目前 session 中保存的匿名 token。
        /// </summary>
        private string GetStoredAnonymousToken()
        {
            return HttpContext.Current?.Session?[SessionAnonymousTokenKey] as string;
        }

        /// <summary>
        /// 取得目前 session 中保存的 JWT token。
        /// </summary>
        private string GetStoredJwtToken()
        {
            return HttpContext.Current?.Session?[SessionJwtTokenKey] as string;
        }

        /// <summary>
        /// 取得目前 session 中保存的 Ezway 使用者代碼。
        /// </summary>
        private string GetStoredUserId()
        {
            return HttpContext.Current?.Session?[SessionUserIdKey] as string;
        }

        /// <summary>
        /// 取得目前 session 中保存的報關業者統編。
        /// </summary>
        private string GetStoredBrokerBan()
        {
            return HttpContext.Current?.Session?[SessionBrokerBanKey] as string;
        }

        /// <summary>
        /// 將匿名 token 保存到目前 session。
        /// </summary>
        private void StoreAnonymousToken(string token)
        {
            if (HttpContext.Current?.Session != null)
            {
                HttpContext.Current.Session[SessionAnonymousTokenKey] = NormalizeBearerToken(token);
            }
        }

        /// <summary>
        /// 清除目前 session 中的匿名 token。
        /// </summary>
        private void ClearAnonymousToken()
        {
            if (HttpContext.Current?.Session != null)
            {
                HttpContext.Current.Session[SessionAnonymousTokenKey] = null;
            }
        }

        /// <summary>
        /// 將登入成功後的 JWT、使用者代碼與報關業者資訊保存到 session。
        /// </summary>
        private void StoreAuthenticatedSession(string jwtToken, string userId, string brokerBan)
        {
            if (HttpContext.Current?.Session == null)
            {
                return;
            }

            HttpContext.Current.Session[SessionJwtTokenKey] = NormalizeBearerToken(jwtToken);
            HttpContext.Current.Session[SessionUserIdKey] = userId?.Trim();
            HttpContext.Current.Session[SessionBrokerBanKey] = brokerBan?.Trim();
        }

        /// <summary>
        /// 清除目前 session 中的 Ezway 登入資訊。
        /// </summary>
        private void ClearAuthenticatedSession()
        {
            if (HttpContext.Current?.Session == null)
            {
                return;
            }

            HttpContext.Current.Session[SessionJwtTokenKey] = null;
            HttpContext.Current.Session[SessionUserIdKey] = null;
            HttpContext.Current.Session[SessionBrokerBanKey] = null;
        }

        /// <summary>
        /// 移除 Bearer 前綴並回傳乾淨的 token 字串。
        /// </summary>
        private static string NormalizeBearerToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            token = token.Trim();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return token.Substring(7).Trim();
            }

            return token;
        }

        private sealed class EccsApiResponse<T>
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("msg")]
            public string Msg { get; set; }

            [JsonProperty("returnMsg")]
            public string ReturnMsg { get; set; }

            [JsonProperty("data")]
            public T Data { get; set; }
        }

        private sealed class TokenData
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }

        private sealed class CaptchaData
        {
            [JsonProperty("image")]
            public string Image { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; }
        }

        private sealed class AnnouncementItem
        {
            [JsonProperty("context")]
            public string Context { get; set; }
        }

        private sealed class LoginData
        {
            [JsonProperty("token")]
            public string Token { get; set; }

            [JsonProperty("userId")]
            public string UserId { get; set; }

            [JsonProperty("idNo")]
            public string IdNo { get; set; }

            [JsonProperty("brokerBan")]
            public string BrokerBan { get; set; }
        }

        private sealed class SignHeaders
        {
            public string Timestamp { get; set; }

            public string Sign { get; set; }
        }

        private sealed class EzwaySessionExpiredException : Exception
        {
        }
    }
}
