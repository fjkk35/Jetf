namespace Service.Services.EtlClearanceDetails.Domain
{
    /// <summary>
    /// Coupang 商品資料模型
    /// </summary>
    public class CoupangGoodsModel
    {
        /// <summary>
        /// 商品描述
        /// </summary>
        public string Goods { get; set; }

        /// <summary>
        /// 產地國家代碼
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// 產品中文名稱
        /// </summary>
        public string ProductName { get; set; }
    }
}
