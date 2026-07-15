using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 提供已登入但缺少功能權限時顯示的頁面。
    /// </summary>
    public class AccessDeniedController : Controller
    {
        /// <summary>
        /// 顯示權限不足提示。
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }
    }
}
