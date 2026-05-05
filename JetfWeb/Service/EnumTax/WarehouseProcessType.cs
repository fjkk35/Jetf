using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum WarehouseProcessType : byte
    {
        /// <summary>
        /// 已出庫
        /// </summary>
        [Description("已出庫")]
        OutBound = 1,

        /// <summary>
        /// 待銷毀
        /// </summary>
        [Description("待銷毀")]
        PendingDisposal = 2,

        /// <summary>
        /// 待退運
        /// </summary>
        [Description("待退運")]
        PendingReturn = 3,

        /// <summary>
        /// 已銷毀
        /// </summary>
        [Description("已銷毀")]
        Disposed = 4,

        /// <summary>
        /// 已退運
        /// </summary>
        [Description("已退運")]
        Returned = 5,

        /// <summary>
        /// 問題件暫留庫
        /// </summary>
        [Description("問題件暫留庫")]
        OnHold = 6
    }
}
