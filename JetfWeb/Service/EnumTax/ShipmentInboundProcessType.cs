using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum ShipmentInboundProcessType : byte
    {
        /// <summary>
        /// 開新單號重出
        /// </summary>
        [Description("開新單號重出")]
        NewTrackingNo = 1,

        /// <summary>
        /// 原單轉出
        /// </summary>
        [Description("原單轉出")]
        TransferFromOriginal = 2,

        /// <summary>
        /// 退回現場
        /// </summary>
        [Description("退回現場")]
        ReturnToSite = 3,

        /// <summary>
        /// 自提
        /// </summary>
        [Description("自提")]
        SelfPickup = 4,

        /// <summary>
        /// 銷毀
        /// </summary>
        [Description("銷毀")]
        Destroy = 5,

        /// <summary>
        /// 加入退運清單
        /// </summary>
        [Description("加入退運清單")]
        AddToReturnShipment = 6,

        /// <summary>
        /// 開箱確認內容物狀況
        /// </summary>
        [Description("開箱確認內容物狀況")]
        InspectContents = 7,

        /// <summary>
        /// 確認外箱面單
        /// </summary>
        [Description("確認外箱面單")]
        ConfirmOuterLabel = 8,

        /// <summary>
        /// 暫存資料
        /// </summary>
        [Description("暫存資料")]
        TempData = 9,

        /// <summary>
        /// 過系統轉出
        /// </summary>
        [Description("過系統轉出")]
        TransferBySystem = 10

    }
}
