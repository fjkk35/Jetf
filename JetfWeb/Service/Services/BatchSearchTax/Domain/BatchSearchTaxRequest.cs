namespace Service.Services.BatchSearchTax.Domain
{
    /// <summary>
    /// 批量查詢請求模型
    /// </summary>
    public class BatchSearchTaxRequest
    {
        /// <summary>
        /// 物流貨號列表（換行分隔）
        /// </summary>
        public string DlvInvList { get; set; }
    }
}
