namespace Service.Models
{
    /// <summary>
    /// 稅金總表及明細表匯出結果。
    /// </summary>
    public sealed class DownloadIncludeTaxExportResult
    {
        /// <summary>
        /// Excel 檔案內容。
        /// </summary>
        public byte[] FileBytes { get; set; }

        /// <summary>
        /// Excel 檔案名稱。
        /// </summary>
        public string FileName { get; set; }
    }
}
