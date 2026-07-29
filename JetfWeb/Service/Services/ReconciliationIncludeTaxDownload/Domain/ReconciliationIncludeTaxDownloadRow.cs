using System;

namespace Service.Services.ReconciliationIncludeTaxDownload.Domain
{
    /// <summary>
    /// 包稅客戶明細下載資料列。
    /// </summary>
    public sealed class ReconciliationIncludeTaxDownloadRow
    {
        /// <summary>
        /// 出倉時間。
        /// </summary>
        public DateTime? OutDateTime { get; set; }

        /// <summary>
        /// 報關類別。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶代號。
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 清關袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        public int? TaxBase { get; set; }
    }
}
