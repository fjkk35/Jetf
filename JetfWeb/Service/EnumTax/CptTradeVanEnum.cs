using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum CptTradeVanEnum
    {
        /// <summary>
        /// 收單
        /// </summary>
        [Description("海運-收單查詢")]
        ReceiveOrder,

        /// <summary>
        /// 錯單
        /// </summary>
        [Description("海運-錯單查詢")]
        ErrorOrder,

        /// <summary>
        /// 銷艙
        /// </summary>
        [Description("海運-銷艙率查詢")]
        CargoManifest,

        /// <summary>
        /// 海運-主號查詢(海快作業)
        /// </summary>
        [Description("海運-主號查詢(海快作業)")]
        SeaMainNumber,

        /// <summary>
        /// 海運-收單查詢(海快作業)
        /// </summary>
        [Description("海運-收單查詢(海快作業)")]
        SeaReceiveOrderWork,

        /// <summary>
        /// 海運-錯單查詢(海快作業)
        /// </summary>
        [Description("海運-錯單查詢(海快作業)")]
        ErrorOrderWork,

        /// <summary>
        /// 海運-主號刪除(海快作業)
        /// </summary>
        [Description("海運-主號刪除(海快作業)")]
        DeleteSeaMainNumber,

        /// <summary>
        /// 空運-錯單
        /// </summary>
        [Description("空運-錯單查詢")]
        EtlErrorOrder,

        /// <summary>
        /// 空運-正式報單查詢
        /// </summary>
        [Description("空運-正式報單查詢")]
        EtlClearanceOrder,

    }
}
