using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum ShipmentInboundProcessTransNo : byte
    {
        /// <summary>
        /// 新竹物流
        /// </summary>
        [Description("新竹物流")]
        Hct = 1,

        /// <summary>
        /// 黑貓
        /// </summary>
        [Description("黑貓")]
        TCat = 2,

        /// <summary>
        /// 7-11
        /// </summary>
        [Description("7-11")]
        SevenEleven = 3,

        /// <summary>
        /// 郵局
        /// </summary>
        [Description("郵局")]
        Post = 4,
    }
}
