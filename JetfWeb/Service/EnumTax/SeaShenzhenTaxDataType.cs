using System.ComponentModel;

namespace Service.EnumTax
{
    /// <summary>
    /// 新遞深圳稅單資料來源報關行。
    /// </summary>
    public enum SeaShenzhenTaxDataType
    {
        /// <summary>
        /// 捷豐
        /// </summary>
        [Description("捷豐")]
        Jetf = 1,

        /// <summary>
        /// -新遞。
        /// </summary>
        [Description("新遞")]
        Shenzhen = 2,
    }
}
