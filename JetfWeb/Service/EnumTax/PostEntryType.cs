using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    public enum PostEntryType
    {
        /// <summary>
        /// X2
        /// </summary>
        [Description("X2")]
        X2,

        /// <summary>
        /// X3
        /// </summary>
        [Description("X3")]
        X3,

        /// <summary>
        /// 移倉
        /// </summary>
        [Description("移倉")]
        移倉,

        /// <summary>
        /// 轉移倉
        /// </summary>
        [Description("轉移倉")]
        轉移倉,

        /// <summary>
        /// G1
        /// </summary>
        [Description("G1")]
        G1,

        /// <summary>
        /// 轉G1
        /// </summary>
        [Description("轉G1")]
        轉G1,
    }
}
