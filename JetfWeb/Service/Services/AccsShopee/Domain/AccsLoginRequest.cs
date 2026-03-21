using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.AccsShopee.Domain
{
    /// <summary>
    /// Accs 登入請求
    /// </summary>
    public class AccsLoginRequest
    {
        /// <summary>
        /// 使用者帳號
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 使用者密碼
        /// </summary>
        public string UserWd { get; set; }

        /// <summary>
        /// 驗證碼
        /// </summary>
        public string VerifyCode { get; set; }
    }
}
