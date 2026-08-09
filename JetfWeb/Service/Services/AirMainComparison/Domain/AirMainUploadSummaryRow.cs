namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 空運主號上傳 Excel 的主號2摘要資料列。
    /// </summary>
    public class AirMainUploadSummaryRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string Mwb { get; set; }

        /// <summary>
        /// 總件數。
        /// </summary>
        public string TotalPiece { get; set; }

        /// <summary>
        /// 傳輸時間。
        /// </summary>
        public string TransmissionTime { get; set; }

        /// <summary>
        /// 進口日期。
        /// </summary>
        public string ImportDate { get; set; }

        /// <summary>
        /// 航機班次。
        /// </summary>
        public string FlightNumber { get; set; }
    }
}
