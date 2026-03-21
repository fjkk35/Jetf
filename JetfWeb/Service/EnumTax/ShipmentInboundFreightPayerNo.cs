using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum ShipmentInboundFreightPayerNo : byte
    {
        /// <summary>
        /// 收件人
        /// </summary>
        [Description("收件人")]
        Consignee =1,

        /// <summary>
        /// 客戶
        /// </summary>
        [Description("客戶")]
        Customer = 2,

        /// <summary>
        /// 捷豐
        /// </summary>
        [Description("捷豐")]
        Jetf = 3
    }
}
