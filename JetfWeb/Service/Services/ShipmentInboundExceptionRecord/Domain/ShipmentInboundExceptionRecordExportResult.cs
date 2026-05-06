namespace Service.Services.ShipmentInboundExceptionRecord.Domain
{
    /// <summary>
    /// 異常件紀錄匯出結果。
    /// </summary>
    public class ShipmentInboundExceptionRecordExportResult
    {
        /// <summary>
        /// 匯出檔名。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 匯出檔案內容。
        /// </summary>
        public byte[] FileBytes { get; set; }
    }
}
