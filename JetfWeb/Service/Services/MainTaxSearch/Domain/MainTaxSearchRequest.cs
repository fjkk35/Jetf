namespace Service.Services.MainTaxSearch.Domain
{
    /// <summary>
    /// 主號稅金查詢請求
    /// </summary>
    public class MainTaxSearchRequest
    {
        /// <summary>
        /// 主號清單（換行分隔多筆）
        /// </summary>
        public string MainNumberList { get; set; }
    }
}
