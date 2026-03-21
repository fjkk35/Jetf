using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum UserStatusEnum
    {
        /// <summary>
        /// 停用
        /// </summary>
        [Description("停用")]
        Disable = 0,

        /// <summary>
        /// 啟用
        /// </summary>
        [Description("啟用")]
        Eenable = 1,
    }
}
