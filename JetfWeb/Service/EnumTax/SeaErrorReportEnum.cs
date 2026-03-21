using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum SeaErrorReportEnum
    {
        /// <summary>
        /// 未收單明細
        /// </summary>
        [Description("未收單明細")]
        UnreceivedOrder,

        /// <summary>
        /// 可傳輸明細
        /// </summary>
        [Description("可傳輸明細")]
        Transmittable,

        /// <summary>
        /// 可申報明細(分提單號)
        /// </summary>
        [Description("可申報明細(分提單號)")]
        Declare,
    }
}
