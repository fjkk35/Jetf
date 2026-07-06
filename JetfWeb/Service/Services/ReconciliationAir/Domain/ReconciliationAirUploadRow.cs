namespace Service.Services.ReconciliationAir.Domain
{
    /// <summary>
    /// 空快代收銷帳上傳的單筆列資料。
    /// </summary>
    public sealed class ReconciliationAirUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 類型（FTZ / TACT）。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 納稅義務人統一編號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 營業稅基原始文字。
        /// </summary>
        public string TaxBaseText { get; set; }

        /// <summary>
        /// 營業稅基。
        /// </summary>
        public int TaxBase { get; set; }

        /// <summary>
        /// 稅費金額原始文字。
        /// </summary>
        public string TaxText { get; set; }

        /// <summary>
        /// 稅費金額。
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 失敗原因。
        /// </summary>
        public string FailReason { get; set; }
    }
}
