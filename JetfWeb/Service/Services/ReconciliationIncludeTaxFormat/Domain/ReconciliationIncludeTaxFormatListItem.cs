namespace Service.Services.ReconciliationIncludeTaxFormat.Domain
{
    /// <summary>
    /// 包稅客戶 Excel 匯出格式清單資料。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFormatListItem
    {
        /// <summary>
        /// 格式識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 格式名稱。
        /// </summary>
        public string FormatName { get; set; }

        /// <summary>
        /// 欄位數量。
        /// </summary>
        public int ColumnCount { get; set; }
    }
}
