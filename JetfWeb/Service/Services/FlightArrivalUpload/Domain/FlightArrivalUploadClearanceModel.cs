namespace Service.Services.FlightArrivalUpload.Domain
{
    /// <summary>
    /// A03、B6F 工作表的單筆匯入資料。
    /// </summary>
    public sealed class FlightArrivalUploadClearanceModel
    {
        /// <summary>
        /// 倉儲類型，A03 資料為空字串。
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 主提單號碼。
        /// </summary>
        public string MainNumber { get; set; } = string.Empty;

        /// <summary>
        /// A03 袋號或 B6F 報單號碼。
        /// </summary>
        public string BlNo { get; set; } = string.Empty;

        /// <summary>
        /// 資料來源工作表，為 A03 或 B6F。
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        /// 合併袋號中 0H4 出現的次數，超過一次時才填入。
        /// </summary>
        public string BagCount { get; set; } = string.Empty;
    }
}
