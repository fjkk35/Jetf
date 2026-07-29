using Service.EnumTax;

namespace Service.Services.ReconciliationIncludeTaxFormat.Domain
{
    /// <summary>
    /// 包稅客戶匯出欄位設定請求。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFormatColumnRequest
    {
        /// <summary>
        /// 匯出欄位名稱。
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// 資料來源類型。
        /// </summary>
        public ReconciliationIncludeTaxColumnSourceType SourceType { get; set; }

        /// <summary>
        /// 對應資料欄位代碼。
        /// </summary>
        public string FieldKey { get; set; }

        /// <summary>
        /// 固定值內容。
        /// </summary>
        public string DefaultValue { get; set; }
    }
}
