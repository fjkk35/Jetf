namespace Service.Services.ReconciliationIncludeTaxFormat.Domain
{
    /// <summary>
    /// 可供包稅客戶匯出格式使用的資料欄位。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFieldOption
    {
        /// <summary>
        /// 欄位代碼。
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 畫面顯示名稱。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 對應資料表欄位路徑。
        /// </summary>
        public string DataPath { get; set; }
    }
}
