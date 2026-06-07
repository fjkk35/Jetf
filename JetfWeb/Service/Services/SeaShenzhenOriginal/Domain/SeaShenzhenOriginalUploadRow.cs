using System;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞託運資料 Excel 上傳列。
    /// </summary>
    public class SeaShenzhenOriginalUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }
        public string TrackingNo { get; set; }
        public string BlNo { get; set; }
        public string OrderNo { get; set; }
        public string JetfSerial { get; set; }
        public string TransTimeText { get; set; }
        public DateTime? TransTime { get; set; }
        public string TransName { get; set; }
        public string Importer { get; set; }
        public string ImporterAddress { get; set; }
        public string ImporterPhone { get; set; }
        public string ItemName { get; set; }
        public string CcText { get; set; }
        public double? Cc { get; set; }
        public string QuantityText { get; set; }
        public int? Quantity { get; set; }
        public string GwText { get; set; }
        public decimal? Gw { get; set; }
        public string Memo { get; set; }
        public string Claimant { get; set; }
        /// <summary>
        /// 稅金支付方式代碼。
        /// </summary>
        public string TaxPayment { get; set; }

        /// <summary>
        /// 上傳狀態。
        /// </summary>
        public string UploadStatus { get; set; }
        public string FailFieldName { get; set; }
        public string FailReason { get; set; }
    }
}
