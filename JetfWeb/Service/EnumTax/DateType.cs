using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum DateType : int
    {
        /// <summary>
        /// 工作天
        /// </summary>
        [Description("工作天")]
        WorkDay = 1,

        /// <summary>
        /// 假日
        /// </summary>
        [Description("假日")]
        Holiday = 2,
    }
}
