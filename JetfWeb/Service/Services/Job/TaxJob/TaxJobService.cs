using Dapper;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Service.Extensions;
using Service.Models;
using Service.Services.TaxJob.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class TaxJobService : _BaseService
    {

        /// <summary>
        /// 海運稅金傳送給捷利
        /// </summary>
        /// <returns></returns>
        public async Task RunSeaTaxJobAsync()
        {
            try
            {
                var dataDate = DateTime.Now.AddHours(-12).ToString("yyyyMMdd");

                var seaTaxs = GetSeaTax(dataDate);

                foreach(var seaTax in seaTaxs)
                {
                    var request = new TaxRequestModel
                    {
                        FeeMasterId = seaTax.Id,
                        Date = seaTax.DataDate,
                        TaxNumber = seaTax.Tax_Number,
                        DeclarationNumber = seaTax.Clearance_Number,
                        Bigbagid = seaTax.TrackingNo,
                        Edelno = seaTax.Dlv_Inv,
                        ConsigneeName = seaTax.Recipient,
                        TaxAmount = seaTax.Tax_Amount,
                        ProductName = seaTax.Dlv_Com,
                        PayObject = "捷丰国际物流股份有限公司",
                        Type= "海运"
                    };

                    if (string.IsNullOrEmpty(request.TaxNumber))
                    {
                        //無稅金資料
                        await SaveNoTaxNumber(request);
                        //跳過此筆
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
        /// 空運稅金傳送給捷利
        /// </summary>
        /// <returns></returns>
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
                    var request = new TaxRequestModel
                    {
                        FeeMasterId = etlTax.Id,
                        Date = etlTax.DataDate,
                        TaxNumber = etlTax.Tax_Number,
                        DeclarationNumber = etlTax.Clearance_Number,
                        Bigbagid = etlTax.Bag_Number,
                        Edelno = etlTax.Dlv_Inv,
                        ConsigneeName = etlTax.Recipient,
                        TaxAmount = etlTax.Tax_Amount,
                        ProductName = etlTax.Trans_Name,
                        PayObject = "捷丰国际物流股份有限公司",
                        Type = "空运"
                    };


                    if (string.IsNullOrEmpty(request.TaxNumber))
                    {
                        //無稅金資料
                        await SaveNoTaxNumber(request);
                        //跳過此筆
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
                        )
                      ";

            return conn.Query<SeaTaxModel>(sql,
                new
                {
                    DataDate = dataDate
                },commandTimeout: 300).ToList();

        }

        public List<EtlTaxModel> GetEtlTax(string sDate,string eDate)
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
                        )
                      ";

            return conn.Query<EtlTaxModel>(sql,
                new
                {
                    SDate = sDate,
                    EDate = eDate,
                }, commandTimeout: 300).ToList();

        }

        public async Task PostTaxAsync(TaxRequestModel request)
        {
            int retryCount = 0;
            const int maxRetryAttempts = 3;

            TaxResponseModel taxResponse = null;
            while (retryCount < maxRetryAttempts)
            {
                try
                {
                    using (var _httpClient = new HttpClient())
                    {
                        var secret = "JFTAX";
                        var url = "https://wl.sjlexpress.com/delivery/JFReceive/taxInfo";

                        var jsonContent = JsonConvert.SerializeObject(request);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var sign = ToMD5($"{secret}{jsonContent}");
                        //發送 POST 請求
                        _httpClient.DefaultRequestHeaders.Add("sign", sign);
                        _httpClient.DefaultRequestHeaders.Add("secret", secret);
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        using (HttpResponseMessage response = await _httpClient.PostAsync(url, content))
                        {
                            // 檢查請求是否成功
                            if (response.IsSuccessStatusCode)
                            {
                                var result = await response.Content.ReadAsStringAsync();
                                taxResponse = JsonConvert.DeserializeObject<TaxResponseModel>(result);
                                taxResponse.FeeMasterId = request.FeeMasterId;
                                //成功後離開迴圈
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    taxResponse = new TaxResponseModel()
                    {
                        FeeMasterId = request.FeeMasterId,
                        Message = ex.Message.Truncate(200),
                    };

                    await Task.Delay(1000);
                }

                retryCount++;
            }

            // 將結果寫入資料庫
           await SaveTaxResponse(taxResponse);
        }


        private async Task SaveTaxResponse(TaxResponseModel taxResponse)
        {
            if (taxResponse == null)
                return;

            var sql = @"
                INSERT INTO [jetf].[dbo].SjlTaxResponse (FeeMasterId,Code, Data, Message, Status, Time)
                VALUES (@FeeMasterId,@Code, @Data, @Message, @Status, @Time)";

            await conn.ExecuteAsync(sql, taxResponse);
        }

        private async Task SaveNoTaxNumber(TaxRequestModel request)
        {
            if (request == null)
                return;

            var sql = @"
                INSERT INTO [jetf].[dbo].SjlNoTaxNumber (Type,FeeMasterId)
                VALUES (@Type,@FeeMasterId)";

            await conn.ExecuteAsync(sql, request);
        }
    }
}
