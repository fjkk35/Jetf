using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum FtzReptileUrl
    {
        /// <summary>
        /// 分號
        /// </summary>
        [Description("分號")]
        Hwb = 1,

        /// <summary>
        /// 併袋號
        /// </summary>
        [Description("併袋號")]
        BagNo = 2
    }
}
