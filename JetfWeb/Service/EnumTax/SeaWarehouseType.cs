using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    /// <summary>
    /// 海運倉別
    /// </summary>
    public enum SeaWarehouseType
    {
        [Description("TPCT(捷豐)")]
        TPCT,

        [Description("高雄郵聯(億興)")]
        IPOST,

        [Description("高雄郵聯(全旺)")]
        CHWN,

        [Description("高雄郵聯(捷豐)")]
        JFKH,
    }
}
