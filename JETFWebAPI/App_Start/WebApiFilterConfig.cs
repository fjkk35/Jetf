using System.Web.Http;
using JETFWebAPI.Filters;

namespace JETFWebAPI.App_Start
{
    /// <summary>
    /// WebAPI 全域過濾器設定
    /// </summary>
    public class WebApiFilterConfig
    {
        /// <summary>
        /// 註冊 WebAPI 全域過濾器
        /// </summary>
        /// <param name="config"></param>
        public static void Register(HttpConfiguration config)
        {
            // 註冊 NLog Action 過濾器到所有 WebAPI Controller
            config.Filters.Add(new WebApiNLogActionFilterAttribute());
        }
    }
}