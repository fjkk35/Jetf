namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳比對明細下載檔案資訊。
    /// </summary>
    public sealed class ReconciliationLogisticsComparisonFileResult
    {
        /// <summary>
        /// 暫存檔案識別碼。
        /// </summary>
        public string FileGuid { get; set; }

        /// <summary>
        /// 下載檔名。
        /// </summary>
        public string FileName { get; set; }
    }
}
