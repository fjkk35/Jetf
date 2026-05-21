using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    /// <summary>
    /// 貨件來源
    /// </summary>
    public enum ShipmentInboundSourceType : byte
    {
        /// <summary>
        /// 新竹退件
        /// </summary>
        [Description("新竹退件")]
        Hct = 1,

        /// <summary>
        /// 黑貓退件
        /// </summary>
        [Description("黑貓退件")]
        TCat = 2,

        /// <summary>
        /// 7-11退件
        /// </summary>
        [Description("7-11退件")]
        SevenEleven = 3,

        /// <summary>
        /// 萊爾富退件
        /// </summary>
        [Description("萊爾富退件")]
        Hilife = 4,

        /// <summary>
        /// OK退件
        /// </summary>
        [Description("OK退件")]
        OK = 5,

        /// <summary>
        /// 全家退件
        /// </summary>
        [Description("全家退件")]
        Family = 6,

        /// <summary>
        /// 圓通退件
        /// </summary>
        [Description("圓通退件")]
        Yto = 7,

        [Description("海快現場帶回")]
        SeaSite = 8,

        /// <summary>
        /// 空快現場帶回
        /// </summary>
        [Description("空快現場帶回")]
        EtlSite = 9,

        /// <summary>
        /// 大榮退件
        /// </summary>
        [Description("大榮退件")]
        Ktj = 10,

        /// <summary>
        /// 蝦皮現場帶回
        /// </summary>
        [Description("蝦皮現場帶回")]
        ShopeeSite = 11,
    }
}
