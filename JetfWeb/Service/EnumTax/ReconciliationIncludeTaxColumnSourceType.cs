namespace Service.EnumTax
{
    /// <summary>
    /// 包稅客戶匯出欄位的資料來源類型。
    /// </summary>
    public enum ReconciliationIncludeTaxColumnSourceType
    {
        /// <summary>
        /// 取自費用主檔或費用明細欄位。
        /// </summary>
        Field = 0,

        /// <summary>
        /// 使用格式設定的固定值。
        /// </summary>
        Constant = 1
    }
}
