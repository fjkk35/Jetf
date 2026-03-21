using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum CptStatusEnum
    {
        [Description("全部")]
        All,

        /// <summary>
        /// 未收單
        /// </summary>
        [Description("未收單")]
        UnreceivedOrder,

    }
}
