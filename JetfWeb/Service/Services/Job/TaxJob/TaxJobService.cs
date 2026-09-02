using Dapper;
using Newtonsoft.Json;
using Service.Extensions;
using Service.Models;
using Service.Services.TaxJob.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    /// <summary>
    /// 捷利稅金排程與共用 API 服務。
    /// </summary>
    public class TaxJobService : _BaseService
    {
        private const string SeaTaxType = "海運";
        private const string AirTaxType = "空運";
        private const string PayObject = "捷丰国际物流股份有限公司";
        private const string TaxApiUrl = "https://wl.sjlexpress.com/delivery/JFReceive/taxInfo";
        private const string TaxApiSecret = "JFTAX";
        private const int MaxRetryAttempts = 3;

        /// <summary>
        /// 建立捷利稅金排程與共用 API 服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">Data Center 資料庫內容。</param>
        public TaxJobService(Service.Data.JetfDbContext jetfDbContext, Service.Data.DataCenterDbContext dataCenterDbContext)
            : base(jetfDbContext, dataCenterDbContext)
        {
        }

        /// <summary>
        /// 執行海運稅金排程，將稅金傳送給捷利。
        /// </summary>
        /// <returns>非同步工作。</returns>
        public async Task RunSeaTaxJobAsync()
        {
            try
            {
                var dataDate = DateTime.Now.AddHours(-12).ToString("yyyyMMdd");
                var seaTaxs = GetSeaTax(dataDate);

                foreach (var seaTax in seaTaxs)
                {
                    var request = BuildSeaTaxRequest(seaTax);

                    if (string.IsNullOrWhiteSpace(request.TaxNumber))
                    {
                        await SaveNoTaxNumberAsync(request);
                        continue;
                    }

                    await PostTaxAsync(request);
                }
            }
            catch (Exception ex)
            {
                WriteJobErrorLog("捷利海運稅金", ex);
            }
        }

        /// <summary>
        /// 執行空運稅金排程，將稅金傳送給捷利。
        /// </summary>
        /// <returns>非同步工作。</returns>
        public async Task RunEtlTaxJobAsync()
        {
            try
            {
                var date = DateTime.Now.AddHours(-12);
                var sDate = date.AddDays(-2).ToString("yyyyMMdd");
                var eDate = date.ToString("yyyyMMdd");
                var etlTaxs = GetEtlTax(sDate, eDate);

                foreach (var etlTax in etlTaxs)
                {
                    var request = BuildEtlTaxRequest(etlTax);

                    if (string.IsNullOrWhiteSpace(request.TaxNumber))
                    {
                        await SaveNoTaxNumberAsync(request);
                        continue;
                    }

                    await PostTaxAsync(request);
                }
            }
            catch (Exception ex)
            {
                WriteJobErrorLog("捷利空運稅金", ex);
            }
        }

        /// <summary>
        /// 取得指定日期、尚未成功回傳的海運稅金。
        /// </summary>
        /// <param name="dataDate">資料日期。</param>
        /// <returns>海運稅金資料。</returns>
        public List<SeaTaxModel> GetSeaTax(string dataDate)
        {
            var sql = @"
                        select a.Id,DATADATE,a.DLV_COM,TRACKINGNO,DLV_INV,CLEARANCE_NUMBER,a.RECIPIENT,b.TAX_NUMBER,b.TAX_AMOUNT from [jetf].[dbo].[FEE_MASTER] a
                        left join DATA_CENTER.dbo.CLEARANCE_TAX b on a.TRACKINGNO = b.BAG_NUMBER
                        where DATADATE=@DataDate
                        and a.SOURCE_TYPE='1'
                        and a.Download='1'
                        and CUSTOMER in ('CN00060','CN00063')
                        and INCLUDE_TAX in ('C')
                        and NOT EXISTS
                        (
                             select 1 from [jetf].[dbo].SjlTaxResponse
                             where FeeMasterId = a.ID and STATUS=1
                        )";

            return conn.Query<SeaTaxModel>(sql,
                new
                {
                    DataDate = dataDate
                }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 取得指定日期區間、尚未成功回傳的空運稅金。
        /// </summary>
        /// <param name="sDate">開始日期。</param>
        /// <param name="eDate">結束日期。</param>
        /// <returns>空運稅金資料。</returns>
        public List<EtlTaxModel> GetEtlTax(string sDate, string eDate)
        {
            var sql = @"
                        select a.Id,DATADATE,a.BAG_NUMBER,d.TRANS_NAME,TRACKINGNO,DLV_INV,CLEARANCE_NUMBER,a.RECIPIENT,isnull(b.TAX_NUMBER,c.TAX_NUMBER) as TAX_NUMBER,isnull(b.TAX_AMOUNT,c.TAX_AMOUNT) as TAX_AMOUNT from [jetf].[dbo].[FEE_MASTER] a
                        left join DATA_CENTER.dbo.CLEARANCE_TAX b on a.TRACKINGNO = b.MERGE_NUMBER
                        left join DATA_CENTER.dbo.CLEARANCE_TAX c on a.BAG_NUMBER = c.MERGE_NUMBER
                        left join [jetf].[dbo].[View_CustomerTrans] d on a.DLV_COM = d.TRANS_NO
                        where DATADATE between @SDate and @EDate
                        and a.SOURCE_TYPE='3'
                        and INCLUDE_TAX in ('Y','C')
                        and CUSTOMER in ('00001','00002','00019','00054','00078')
                        and NOT EXISTS
                        (
                           select 1 from [jetf].[dbo].SjlTaxResponse
                           where FeeMasterId = a.ID and STATUS=1
                        )";

            return conn.Query<EtlTaxModel>(sql,
                new
                {
                    SDate = sDate,
                    EDate = eDate
                }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 呼叫捷利稅金 API 並保存一次回覆結果。
        /// </summary>
        /// <param name="request">稅金回傳資料。</param>
        /// <param name="createdOpe">操作帳號；排程呼叫時可不填。</param>
        /// <returns>捷利 API 回覆。</returns>
        public async Task<TaxResponseModel> PostTaxAsync(TaxRequestModel request, string createdOpe = null)
        {
            if (request == null)
            {
                return null;
            }

            var taxResponse = await CallTaxApiWithRetryAsync(request);

            // API 呼叫完成後只在這裡寫入一次，排程與手動回傳共用此流程。
            await SaveTaxResponse(taxResponse, createdOpe);
            return taxResponse;
        }

        /// <summary>
        /// 將海運稅金資料轉換成捷利 API 請求格式。
        /// </summary>
        /// <param name="tax">海運稅金資料。</param>
        /// <returns>捷利 API 請求資料。</returns>
        public TaxRequestModel BuildSeaTaxRequest(SeaTaxModel tax)
        {
            return new TaxRequestModel
            {
                FeeMasterId = tax.Id,
                Date = tax.DataDate,
                TaxNumber = tax.Tax_Number,
                DeclarationNumber = tax.Clearance_Number,
                Bigbagid = tax.TrackingNo,
                Edelno = tax.Dlv_Inv,
                ConsigneeName = tax.Recipient,
                TaxAmount = tax.Tax_Amount,
                ProductName = tax.Dlv_Com,
                PayObject = PayObject,
                Type = SeaTaxType
            };
        }

        /// <summary>
        /// 將空運稅金資料轉換成捷利 API 請求格式。
        /// </summary>
        /// <param name="tax">空運稅金資料。</param>
        /// <returns>捷利 API 請求資料。</returns>
        public TaxRequestModel BuildEtlTaxRequest(EtlTaxModel tax)
        {
            return new TaxRequestModel
            {
                FeeMasterId = tax.Id,
                Date = tax.DataDate,
                TaxNumber = tax.Tax_Number,
                DeclarationNumber = tax.Clearance_Number,
                Bigbagid = tax.Bag_Number,
                Edelno = tax.Dlv_Inv,
                ConsigneeName = tax.Recipient,
                TaxAmount = tax.Tax_Amount,
                ProductName = tax.Trans_Name,
                PayObject = PayObject,
                Type = AirTaxType
            };
        }

        /// <summary>
        /// 呼叫捷利稅金 API，失敗時依設定次數重試。
        /// </summary>
        /// <param name="request">捷利 API 請求資料。</param>
        /// <returns>捷利 API 回覆資料。</returns>
        private async Task<TaxResponseModel> CallTaxApiWithRetryAsync(TaxRequestModel request)
        {
            TaxResponseModel taxResponse = null;

            for (var retryCount = 0; retryCount < MaxRetryAttempts; retryCount++)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var jsonContent = JsonConvert.SerializeObject(request);
                        using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                        {
                            var sign = ToMD5($"{TaxApiSecret}{jsonContent}");
                            httpClient.DefaultRequestHeaders.Add("sign", sign);
                            httpClient.DefaultRequestHeaders.Add("secret", TaxApiSecret);
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                            using (var response = await httpClient.PostAsync(TaxApiUrl, content))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    var result = await response.Content.ReadAsStringAsync();
                                    taxResponse = JsonConvert.DeserializeObject<TaxResponseModel>(result);
                                    if (taxResponse == null)
                                    {
                                        throw new Exception("捷利 API 回傳內容為空");
                                    }

                                    taxResponse.FeeMasterId = request.FeeMasterId;
                                    return taxResponse;
                                }

                                taxResponse = new TaxResponseModel
                                {
                                    FeeMasterId = request.FeeMasterId,
                                    Message = $"捷利 API HTTP {(int)response.StatusCode} {response.ReasonPhrase}".Truncate(200)
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    taxResponse = new TaxResponseModel
                    {
                        FeeMasterId = request.FeeMasterId,
                        Message = ex.Message.Truncate(200)
                    };
                }

                if (retryCount < MaxRetryAttempts - 1)
                {
                    await Task.Delay(1000);
                }
            }

            return taxResponse;
        }

        /// <summary>
        /// 儲存捷利稅金 API 回覆結果。
        /// </summary>
        /// <param name="taxResponse">捷利 API 回覆資料。</param>
        /// <param name="createdOpe">操作帳號。</param>
        /// <returns>非同步寫入工作。</returns>
        private async Task SaveTaxResponse(TaxResponseModel taxResponse, string createdOpe)
        {
            if (taxResponse == null)
            {
                return;
            }

            var sql = @"
                INSERT INTO [jetf].[dbo].SjlTaxResponse
                    (FeeMasterId, Code, Data, Message, Status, Time, CreatedOpe)
                VALUES
                    (@FeeMasterId, @Code, @Data, @Message, @Status, @Time, @CreatedOpe)";

            await conn.ExecuteAsync(sql, new
            {
                taxResponse.FeeMasterId,
                taxResponse.Code,
                taxResponse.Data,
                taxResponse.Message,
                taxResponse.Status,
                taxResponse.Time,
                CreatedOpe = NormalizeCreatedOpe(createdOpe)
            });
        }

        /// <summary>
        /// 儲存沒有稅單號碼的稅金資料。
        /// </summary>
        /// <param name="request">捷利 API 請求資料。</param>
        /// <returns>非同步寫入工作。</returns>
        public async Task SaveNoTaxNumberAsync(TaxRequestModel request)
        {
            if (request == null)
            {
                return;
            }

            var sql = @"
                INSERT INTO [jetf].[dbo].SjlNoTaxNumber (Type, FeeMasterId)
                VALUES (@Type, @FeeMasterId)";

            await conn.ExecuteAsync(sql, request);
        }

        /// <summary>
        /// 將操作帳號整理成資料庫欄位允許的長度。
        /// </summary>
        /// <param name="createdOpe">原始操作帳號。</param>
        /// <returns>整理後的操作帳號。</returns>
        private string NormalizeCreatedOpe(string createdOpe)
        {
            if (string.IsNullOrWhiteSpace(createdOpe))
            {
                return null;
            }

            var normalized = createdOpe.Trim();
            return normalized.Length > 10 ? normalized.Substring(0, 10) : normalized;
        }

    }
}
