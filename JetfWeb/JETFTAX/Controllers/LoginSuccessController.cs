using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// Provides a common landing page for authenticated users.
    /// </summary>
    public class LoginSuccessController : Controller
    {
        /// <summary>
        /// Displays the post-login page without requiring a role permission.
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }
    }
}
