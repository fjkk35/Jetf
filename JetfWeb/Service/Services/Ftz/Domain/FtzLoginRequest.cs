using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// Ftz 登入請求
    /// </summary>
    public class FtzLoginRequest
    {
        /// <summary>
        /// 使用者帳號
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 使用者密碼
        /// </summary>
        public string UserPd { get; set; }
    }
}
