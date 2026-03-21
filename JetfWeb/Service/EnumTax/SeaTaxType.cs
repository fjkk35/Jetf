using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum SeaTaxType
    {
        [Description("TPCT-台北貨櫃")]
        TPCT,

        [Description("TIPC-台灣港務")]
        TIPC,

        [Description("IPOST-高雄郵聯(億興)")]
        IPOST,

        [Description("CHWN-高雄郵聯(全旺)")]
        CHWN,

        [Description("JFKH-高雄郵聯(捷豐)")]
        JFKH,

        [Description("WAHA-萬海")]
        WAHA,

        [Description("UNIJ-連捷")]
        UNIJ,

        [Description("JFKL-基隆港務(捷豐)")]
        JFKL,
    }
}
