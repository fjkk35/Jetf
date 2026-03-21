using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Service.Services
{
    /// <summary>
    /// 使用者上下文服務 - 提供當前登入使用者相關資訊
    /// </summary>
    public class UserContextService
    {
        /// <summary>
        /// 取得當前使用者ID
        /// </summary>
        /// <returns></returns>
        public static string GetUserId()
        {
            try
            {
                var httpContext = HttpContext.Current;
                if (httpContext?.Session?["user_id"] != null)
                {
                    return httpContext.Session["user_id"].ToString();
                }
            }
            catch (Exception)
            {
                // 忽略例外，回傳預設值
            }
            return "";
        }
    }

}