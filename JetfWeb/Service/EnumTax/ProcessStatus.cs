using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum ProcessStatus
    {
        /// <summary>
        /// 處置說明
        /// </summary>
        [Description("處置說明")]
        Process = 1,

        /// <summary>
        /// 已結案
        /// </summary>
        [Description("已結案")]
        Finish = 2,
    }
}
