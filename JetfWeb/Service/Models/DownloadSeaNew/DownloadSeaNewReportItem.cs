namespace Service.Models.DownloadSeaNew
{
    /// <summary>
    /// 海運下載報表資料列。
    /// </summary>
    public sealed class DownloadSeaNewReportItem
    {
        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 清關袋號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 運單號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅金一。
        /// </summary>
        public int Tax1 { get; set; }

        /// <summary>
        /// 稅金二。
        /// </summary>
        public int Tax2 { get; set; }

        /// <summary>
        /// 應代收稅金。
        /// </summary>
        public int ToDlvCod { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 稅金類型。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 合併註記。
        /// </summary>
        public string Combine { get; set; }

        /// <summary>
        /// 貨件類型。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string DlvCom { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }
    }
}
