using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.EnumTax
{
    /// <summary>
    /// 貨況查詢處理說明
    /// </summary>
    public enum CargoProcessType
    {
        [Description("貨況")]
        Cargo = 1,

        /// <summary>
        /// 退運
        /// </summary>
        [Description("退運")]
        Return = 2,

        /// <summary>
        ///錯單公司名義收單
        /// </summary>
        [Description("錯單公司名義收單")]
        ErrorOrderCompany = 3,

        /// <summary>
        /// 現場轉出
        /// </summary>
        [Description("現場轉出")]
        Transferred = 4,

        /// <summary>
        /// 公司名義不回艙
        /// </summary>
        [Description("公司名義不回艙")]
        NoReturnCompany = 5,
    }
}
