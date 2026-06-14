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
        /// 翔賀
        /// </summary>
        [Description("翔賀")]
        XiangHe = 2,

        /// <summary>
        /// 音速
        /// </summary>
        [Description("音速")]
        YinSu = 3,

        /// <summary>
        /// 祥和
        /// </summary>
        [Description("祥和")]
        HsiangHo = 4,

        /// <summary>
        /// 九龍
        /// </summary>
        [Description("九龍")]
        Kowloon = 5,

        /// <summary>
        /// 億光行
        /// </summary>
        [Description("億光行")]
        YiGuangHang = 6,
    }
}
