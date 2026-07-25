using System.ComponentModel;

namespace Service.EnumTax
{
    /// <summary>
    /// 物流銷帳支援的物流公司。
    /// </summary>
    public enum ReconciliationLogisticsCompany
    {
        /// <summary>
        /// 新竹物流。
        /// </summary>
        [Description("新竹物流")]
        Hct = 1,

        /// <summary>
        /// 7-11。
        /// </summary>
        [Description("7-11")]
        SevenEleven = 2,

        /// <summary>
        /// 客樂得。
        /// </summary>
        [Description("客樂得")]
        Kelede = 3,

        /// <summary>
        /// 大榮。
        /// </summary>
        [Description("大榮")]
        Ktj = 4,

        /// <summary>
        /// 超峰。
        /// </summary>
        [Description("超峰")]
        TaixinStar = 5,

        /// <summary>
        /// 現金。
        /// </summary>
        [Description("現金")]
        Cash = 6,

        /// <summary>
        /// 圓通。
        /// </summary>
        [Description("圓通")]
        Yto = 7,

        /// <summary>
        /// 關貿。
        /// </summary>
        [Description("關貿")]
        TradeVan = 8
    }
}
