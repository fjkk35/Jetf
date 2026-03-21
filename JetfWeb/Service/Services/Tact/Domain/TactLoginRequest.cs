using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Tact.Domain
{
    /// <summary>
    /// Tact 登入請求
    /// </summary>
    public class TactLoginRequest
    {
        /// <summary>
        /// 帳號
        /// </summary>
        public string AcctId { get; set; }

        /// <summary>
        /// 密碼
        /// </summary>
        public string AcctPw { get; set; }
    }
}
