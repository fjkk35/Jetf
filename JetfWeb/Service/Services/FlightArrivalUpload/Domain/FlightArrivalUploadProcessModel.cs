namespace Service.Services.FlightArrivalUpload.Domain
{
    /// <summary>
    /// 由 B6F 產生的貨物處理紀錄。
    /// </summary>
    public sealed class FlightArrivalUploadProcessModel
    {
        /// <summary>
        /// 分提單號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 處理類型。
        /// </summary>
        public int ProcessType { get; set; }

        /// <summary>
        /// 處理備註。
        /// </summary>
        public string Remark { get; set; }
    }
}
