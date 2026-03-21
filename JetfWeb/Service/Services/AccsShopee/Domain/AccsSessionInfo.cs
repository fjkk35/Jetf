using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.AccsShopee.Domain
{
    /// <summary>
    /// Accs Session 資訊
    /// </summary>
    public class AccsSessionInfo
    {
        /// <summary>
        /// Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Session Cookie
        /// </summary>
        public string SessionCookie { get; set; }

        /// <summary>
        /// 是否已登入
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// 登入時間
        /// </summary>
        public DateTime LoginTime { get; set; }
    }
}
