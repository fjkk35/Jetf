namespace Service.Services.ReconciliationIncludeTaxDownload.Domain
{
    /// <summary>
    /// 包稅客戶明細下載結果。
    /// </summary>
    public sealed class ReconciliationIncludeTaxDownloadExportResult
    {
        /// <summary>
        /// 下載檔案內容。
        /// </summary>
        public byte[] FileBytes { get; set; }

        /// <summary>
        /// 下載檔案名稱，單一檔案為 xlsx，多檔案為 zip。
        /// </summary>
        public string FileName { get; set; }
    }
}
