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
        /// 託運單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 到付金額。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 稅金金額。
        /// </summary>
        public int Tax { get; set; }

        /// <summary>
        /// 稅金手續費。
        /// </summary>
        public int Fee { get; set; }
    }
}
