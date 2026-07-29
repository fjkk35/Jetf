using Service.Extensions;
using System;
using System.ComponentModel;

namespace Service.EnumTax
{
    /// <summary>
    /// 包稅客戶 Excel 可匯出的資料欄位。
    /// </summary>
    public enum ReconciliationIncludeTaxField
    {
        /// <summary>
        /// 費用主檔出倉時間。
        /// </summary>
        [Description("出倉時間")]
        FeeMaster_OutDateTime,

        /// <summary>
        /// 費用主檔報關類別。
        /// </summary>
        [Description("報關類別")]
        FeeMaster_Type,

        /// <summary>
        /// 費用主檔客戶。
        /// </summary>
        [Description("客戶")]
        FeeMaster_Customer,

        /// <summary>
        /// 費用明細稅單號碼。
        /// </summary>
        [Description("稅單號碼")]
        FeeMasterDetail_TaxNumber,

        /// <summary>
        /// 費用明細主號。
        /// </summary>
        [Description("主號")]
        FeeMasterDetail_MainNumber,

        /// <summary>
        /// 費用明細清關袋號。
        /// </summary>
        [Description("清關袋號")]
        FeeMasterDetail_BagNumber,

        /// <summary>
        /// 費用明細分提單號。
        /// </summary>
        [Description("分提單號")]
        FeeMasterDetail_TrackingNo,

        /// <summary>
        /// 費用明細物流貨號。
        /// </summary>
        [Description("物流貨號")]
        FeeMasterDetail_DlvInv,

        /// <summary>
        /// 費用明細納稅義務人。
        /// </summary>
        [Description("納稅義務人")]
        FeeMasterDetail_TaxPayer,

        /// <summary>
        /// 費用明細稅金。
        /// </summary>
        [Description("稅金")]
        FeeMasterDetail_Tax,

        /// <summary>
        /// 費用明細稅基。
        /// </summary>
        [Description("稅基")]
        FeeMasterDetail_TaxBase,

        /// <summary>
        /// 空快代收銷帳營業稅。
        /// </summary>
        [Description("營業稅")]
        ReconciliationAir_BusinessTax,

        /// <summary>
        /// 空快代收銷帳進口稅。
        /// </summary>
        [Description("進口稅")]
        ReconciliationAir_ImportTax
    }

    /// <summary>
    /// 包稅客戶匯出欄位 enum 的轉換方法。
    /// </summary>
    public static class ReconciliationIncludeTaxFieldExtensions
    {
        /// <summary>
        /// 取得儲存於格式設定的欄位代碼。
        /// </summary>
        /// <param name="field">欄位 enum。</param>
        /// <returns>既有的資料欄位代碼。</returns>
        public static string ToFieldKey(this ReconciliationIncludeTaxField field)
        {
            return field.ToString().Replace('_', '.');
        }

        /// <summary>
        /// 將格式設定欄位代碼轉為 enum。
        /// </summary>
        /// <param name="fieldKey">格式設定欄位代碼。</param>
        /// <param name="field">轉換後的欄位 enum。</param>
        /// <returns>是否轉換成功。</returns>
        public static bool TryParseFieldKey(
            string fieldKey,
            out ReconciliationIncludeTaxField field)
        {
            field = default(ReconciliationIncludeTaxField);
            if (string.IsNullOrWhiteSpace(fieldKey))
            {
                return false;
            }

            return Enum.TryParse(
                       fieldKey.Trim().Replace('.', '_'),
                       true,
                       out field) &&
                   Enum.IsDefined(typeof(ReconciliationIncludeTaxField), field);
        }

        /// <summary>
        /// 取得欄位的中文顯示名稱。
        /// </summary>
        /// <param name="field">欄位 enum。</param>
        /// <returns>欄位中文名稱。</returns>
        public static string ToDisplayName(this ReconciliationIncludeTaxField field)
        {
            return ((Enum)field).ToDescription();
        }
    }
}
