using Dapper;
using Service.EnumTax;
using Service.Models;
using Service.Services.TaxJob.Model;
using Service.Services.SjlTaxResponse.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Services.SjlTaxResponse
{
    /// <summary>
    /// 捷利稅金手動回傳服務。
    /// </summary>
    public class SjlTaxResponseService : _BaseService
    {
        private const string SeaTaxType = "海運";
        private const string AirTaxType = "空運";
        private const int MaxManualDeliveryNumberCount = 2000;

        private readonly TaxJobService _taxJobService;

        /// <summary>
        /// 建立捷利稅金手動回傳服務。
        /// </summary>
        /// <param name="jetfDbContext">Jetf 資料庫內容。</param>
        /// <param name="dataCenterDbContext">Data Center 資料庫內容。</param>
        /// <param name="taxJobService">捷利稅金共用 API 服務。</param>
        public SjlTaxResponseService(
            Service.Data.JetfDbContext jetfDbContext,
            Service.Data.DataCenterDbContext dataCenterDbContext,
            TaxJobService taxJobService)
            : base(jetfDbContext, dataCenterDbContext)
        {
            _taxJobService = taxJobService;
        }

        /// <summary>
        /// 依物流貨號手動回傳捷利稅金。
        /// </summary>
        /// <param name="request">手動回傳條件。</param>
        /// <returns>手動回傳結果。</returns>
        public async Task<ResponseModel> SendManualTaxAsync(SjlTaxManualRequestModel request)
        {
            var createdOpe = GetUserId();
            var taxType = request?.Type?.Trim();
            if (taxType != SeaTaxType && taxType != AirTaxType)
            {
                return new ResponseModel("請選擇海運或空運");
            }

            var deliveryNumbers = NormalizeDeliveryNumbers(request?.DeliveryNumbers);
            if (!deliveryNumbers.Any())
            {
                return new ResponseModel("請輸入物流貨號");
            }

            if (deliveryNumbers.Count > MaxManualDeliveryNumberCount)
            {
                return new ResponseModel($"物流貨號最多輸入 {MaxManualDeliveryNumberCount} 筆");
            }

            try
            {
                var taxRequests = taxType == SeaTaxType
                    ? GetSeaTaxByDeliveryNumbers(deliveryNumbers)
                        .Select(_taxJobService.BuildSeaTaxRequest)
                        .ToList()
                    : GetEtlTaxByDeliveryNumbers(deliveryNumbers)
                        .Select(_taxJobService.BuildEtlTaxRequest)
                        .ToList();

                var result = new SjlTaxManualResultModel
                {
                    RequestedCount = deliveryNumbers.Count,
                    MatchedCount = taxRequests.Count
                };

                foreach (var taxRequest in taxRequests)
                {
                    var deliveryNumber = taxRequest.Edelno;
                    if (string.IsNullOrWhiteSpace(taxRequest.TaxNumber))
                    {
                        await _taxJobService.SaveNoTaxNumberAsync(taxRequest);
                        result.NoTaxCount++;
                        result.Items.Add(new SjlTaxManualResultItemModel
                        {
                            DeliveryNumber = deliveryNumber,
                            Result = "無稅單號",
                            Message = "查得 FEE_MASTER 資料，但沒有稅單號碼，未呼叫 API"
                        });
                        continue;
                    }

                    var taxResponse = await _taxJobService.PostTaxAsync(taxRequest, createdOpe);
                    var isSuccess = IsTaxResponseSuccess(taxResponse);
                    if (isSuccess)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailureCount++;
                    }

                    result.Items.Add(new SjlTaxManualResultItemModel
                    {
                        DeliveryNumber = deliveryNumber,
                        Result = isSuccess ? "成功" : "失敗",
                        Code = taxResponse?.Code,
                        Message = taxResponse?.Message
                    });
                }

                return new ResponseModel
                {
                    status = Status.success,
                    msg = BuildManualResultMessage(result),
                    ReturnObject = result
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel($"捷利稅金回傳失敗：{ex.Message}");
            }
        }

        /// <summary>
        /// 依物流貨號取得海運 FEE_MASTER 稅金資料。
        /// </summary>
        /// <param name="deliveryNumbers">物流貨號清單。</param>
        /// <returns>海運稅金資料。</returns>
        private List<SeaTaxModel> GetSeaTaxByDeliveryNumbers(List<string> deliveryNumbers)
        {
            var sql = @"
                        select a.Id,DATADATE,a.DLV_COM,TRACKINGNO,DLV_INV,CLEARANCE_NUMBER,a.RECIPIENT,b.TAX_NUMBER,b.TAX_AMOUNT from [jetf].[dbo].[FEE_MASTER] a
                        left join DATA_CENTER.dbo.CLEARANCE_TAX b on a.TRACKINGNO = b.BAG_NUMBER
                        where a.DLV_INV in @DlvInvs
                        and a.SOURCE_TYPE='1'
                        and a.Download='1'
                        and CUSTOMER in ('CN00060','CN00063')
                        and INCLUDE_TAX in ('C')";

            return conn.Query<SeaTaxModel>(sql,
                new { DlvInvs = deliveryNumbers }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 依物流貨號取得空運 FEE_MASTER 稅金資料。
        /// </summary>
        /// <param name="deliveryNumbers">物流貨號清單。</param>
        /// <returns>空運稅金資料。</returns>
        private List<EtlTaxModel> GetEtlTaxByDeliveryNumbers(List<string> deliveryNumbers)
        {
            var sql = @"
                        select a.Id,DATADATE,a.BAG_NUMBER,d.TRANS_NAME,TRACKINGNO,DLV_INV,CLEARANCE_NUMBER,a.RECIPIENT,isnull(b.TAX_NUMBER,c.TAX_NUMBER) as TAX_NUMBER,isnull(b.TAX_AMOUNT,c.TAX_AMOUNT) as TAX_AMOUNT from [jetf].[dbo].[FEE_MASTER] a
                        left join DATA_CENTER.dbo.CLEARANCE_TAX b on a.TRACKINGNO = b.MERGE_NUMBER
                        left join DATA_CENTER.dbo.CLEARANCE_TAX c on a.BAG_NUMBER = c.MERGE_NUMBER
                        left join [jetf].[dbo].[View_CustomerTrans] d on a.DLV_COM = d.TRANS_NO
                        where a.DLV_INV in @DlvInvs
                        and a.SOURCE_TYPE='3'
                        and INCLUDE_TAX in ('Y','C')
                        and CUSTOMER in ('00001','00002','00019','00054','00078')";

            return conn.Query<EtlTaxModel>(sql,
                new { DlvInvs = deliveryNumbers }, commandTimeout: 300).ToList();
        }

        /// <summary>
        /// 將輸入的物流貨號依換行切分、去除空白及重複值。
        /// </summary>
        /// <param name="deliveryNumbers">原始物流貨號文字。</param>
        /// <returns>正規化後的物流貨號清單。</returns>
        private List<string> NormalizeDeliveryNumbers(string deliveryNumbers)
        {
            return (deliveryNumbers ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 判斷捷利 API 回覆是否視為成功。
        /// </summary>
        /// <param name="taxResponse">捷利 API 回覆資料。</param>
        /// <returns>成功回傳 true，否則回傳 false。</returns>
        private bool IsTaxResponseSuccess(TaxResponseModel taxResponse)
        {
            return taxResponse != null
                && (taxResponse.Status
                    || taxResponse.Data
                    || taxResponse.Code == "0"
                    || taxResponse.Code == "200");
        }

        /// <summary>
        /// 組合手動回傳的批次處理摘要訊息。
        /// </summary>
        /// <param name="result">批次處理結果。</param>
        /// <returns>處理摘要訊息。</returns>
        private string BuildManualResultMessage(SjlTaxManualResultModel result)
        {
            return $"成功 {result.SuccessCount} 筆，失敗 {result.FailureCount} 筆";
        }
    }
}
