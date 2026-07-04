using Service.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum AuthorityPartner
    {
        [Description("稅金操作")]
        [Sort(10)]
        Tax,

        [Description("新遞")]
        [Sort(15)]
        SeaShenzhen,

        [Description("查詢")]
        [Sort(20)]
        Search,

        [Description("營收")]
        [Sort(30)]
        Income,

        [Description("作業量")]
        [Sort(40)]
        WorkLoad,

        [Description("清關作業")]
        [Sort(50)]
        ClearanceWork,

        [Description("海快作業")]
        [Sort(60)]
        SeaWork,

        [Description("海快正式報關")]
        [Sort(70)]
        SeaClearance,

        [Description("貨件回倉作業")]
        [Sort(80)]
        ShipmentInbound,

        [Description("捷穩通")]
        [Sort(90)]
        Jetft,

        [Description("代收銷帳作業")]
        [Sort(95)]
        Reconciliation,

        [Description("發送訊息")]
        [Sort(100)]
        Send,

        [Description("Line")]
        [Sort(120)]
        Line,

        [Description("會員")]
        [Sort(130)]
        User,



    }
}
