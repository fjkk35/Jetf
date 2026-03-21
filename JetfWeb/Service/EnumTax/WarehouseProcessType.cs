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
        [Description("已出庫")]
        OutBound = 1,

        [Description("待銷毀")]
        PendingDisposal = 2,

        [Description("待退運")]
        PendingReturn = 3,

        [Description("已銷毀")]
        Disposed = 4,

        [Description("已退運")]
        Returned = 5,

        [Description("問題件暫留庫")]
        OnHold = 6
    }
}
