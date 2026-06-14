namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞深圳稅單轉檔異常列。
    /// </summary>
    public class SeaShenzhenTaxTransferExceptionRow
    {
        /// <summary>
        /// 異常原因。
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅單金額。
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 納稅人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 統編。
        /// </summary>
        public string TaxRecId { get; set; }
    }
}