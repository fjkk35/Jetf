using System.ComponentModel;

namespace Service.EnumTax
{
    /// <summary>
    /// 客戶運送類型。
    /// </summary>
    public enum CustomerType
    {
        /// <summary>
        /// 海運。
        /// </summary>
        [Description("海運")]
        SEA,

        /// <summary>
        /// 空運。
        /// </summary>
        [Description("空運")]
        AIR
    }
}
