namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// FTZ 主號查詢上傳 Excel 的主號2 頁籤資料列。
    /// </summary>
    public class FtzMainUploadSummaryRow
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
