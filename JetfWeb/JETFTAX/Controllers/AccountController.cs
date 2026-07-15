using JETFTAX.Models;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using JETFTAX.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using static JETFTAX.Models.AccountViewModel;

namespace JETFTAX.Controllers
{
    public class AccountController : Controller
    {
        // SSO 連結允許的最大時間差，超過 3 分鐘視為過期。
        private const int SsoTimestampToleranceSeconds = 180;
        private readonly AccountService _accountService;

        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        // GET: Account
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult Login(AccountViewModel vm)
        {
            ResponseModel resopnseModel = new ResponseModel();
            //resopnseModel.status = Status.success;
            //resopnseModel.msg = "";
            //Session["user_id"] = "admin";
            //Session["user_name"] = "admin";
            //return Json(resopnseModel, JsonRequestBehavior.AllowGet);
#if DEBUG
            resopnseModel.status = Status.success;
            resopnseModel.msg = "";
            Session["user_id"] = "admin";
            Session["user_name"] = "admin";
            Session["user_partner"] = _accountService.GetAuthority("admin").Item1;
            Session["user_auth"] = _accountService.GetAuthority("admin").Item2;
            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
#endif


            if (TempData["codeLogin"] == null || vm.code != TempData["codeLogin"].ToString())
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = "驗證碼錯誤";
                return Json(resopnseModel, JsonRequestBehavior.AllowGet);
            }

            if (vm.account == null || vm.password == null)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = "帳號或密碼未輸入";
                return Json(resopnseModel, JsonRequestBehavior.AllowGet);
            }

            UserMasterModel model = _accountService.GetUserMaster(vm.account, vm.password);
            resopnseModel.status = model.Status;
            resopnseModel.msg = model.Msg;
            if (model.Status == Status.success)
            {
                SignInUser(model.Id, model.Name);
            }
            //記錄LOG
            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 透過 URL 參數執行 SSO 登入，驗證成功後沿用既有 Session 登入流程。
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult SsoLogin(string userId, string timestamp, string sign)
        {
            // 三個必要參數缺一不可，否則直接回 400。
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(sign))
            {
                return CreateSsoErrorResult(HttpStatusCode.BadRequest, "001", "缺少必要參數");
            }

            // 先做標準化，避免前後空白造成簽章與查詢不一致。
            var normalizedUserId = userId.Trim();
            var normalizedTimestamp = timestamp.Trim();
            var normalizedSign = sign.Trim();

            // timestamp 必須是 Unix Timestamp 秒數格式。
            if (!long.TryParse(normalizedTimestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var requestTimestamp))
            {
                return CreateSsoErrorResult(HttpStatusCode.BadRequest, "002", "Timestamp 格式錯誤");
            }

            // 依規格只接受 3 分鐘內的請求，避免舊連結被重放。
            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTimestamp - requestTimestamp > SsoTimestampToleranceSeconds)
            {
                return CreateSsoErrorResult(HttpStatusCode.Unauthorized, "003", "Timestamp 已過期");
            }

            // 使用相同規則重算簽章，與呼叫端帶入的 sign 做固定時間比對。
            var expectedSign = ComputeSsoSign(normalizedUserId, normalizedTimestamp);
            if (!FixedTimeEquals(normalizedSign, expectedSign))
            {
                return CreateSsoErrorResult(HttpStatusCode.Unauthorized, "004", "Sign 驗證失敗");
            }

            // 簽章驗證通過後，再檢查 USER_MASTER 是否存在且為啟用狀態。
            var user = _accountService.GetActiveUserById(normalizedUserId);
            if (user.Status != Status.success)
            {
                return CreateSsoErrorResult(HttpStatusCode.Forbidden, "005", "使用者不存在或不可登入");
            }

            // 直接沿用既有登入 Session 欄位，讓現有授權邏輯無須調整。
            SignInUser(user.Id, user.Name);
            return RedirectToAction("Index", "LoginSuccess");
        }


        [AllowAnonymous]
        public ActionResult LogOff()
        {
            //清除Session
            Session.Remove("user_id");
            Session.Remove("user_name");
            Session.Remove("user_partner");
            Session.Remove("user_auth");
            return RedirectToAction("Login", "Account");
        }

        /// <summary>
        /// 統一寫入目前系統登入所需的 Session 欄位。
        /// </summary>
        private void SignInUser(string userId, string userName)
        {
            var authority = _accountService.GetAuthority(userId);

            Session["user_id"] = userId;
            Session["user_name"] = userName;
            Session["user_partner"] = authority.Item1;
            Session["user_auth"] = authority.Item2;
        }

        /// <summary>
        /// 建立 SSO API 的錯誤回應格式與 HTTP 狀態碼。
        /// </summary>
        private ActionResult CreateSsoErrorResult(HttpStatusCode statusCode, string code, string message)
        {
            Response.StatusCode = (int)statusCode;
            return Json(new
            {
                code,
                message
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 依 userId 與 timestamp 組成原文，計算 HMACSHA256 小寫十六進位簽章。
        /// </summary>
        private static string ComputeSsoSign(string userId, string timestamp)
        {
            var secretKey = ConfigurationManager.AppSettings["SsoLoginSecretKey"];
            var rawData = $"userId={userId}&timestamp={timestamp}";
            var secretBytes = Encoding.UTF8.GetBytes(secretKey ?? string.Empty);
            var rawDataBytes = Encoding.UTF8.GetBytes(rawData);

            using (var hmac = new HMACSHA256(secretBytes))
            {
                var hash = hmac.ComputeHash(rawDataBytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var item in hash)
                {
                    builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// 使用固定時間比對避免因字串提早中斷而洩漏簽章差異資訊。
        /// </summary>
        private static bool FixedTimeEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
        public class LoginFilter : FilterAttribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationContext filterContext)
            {
                var loginUser = UserContextService.GetUserId();
                //When user has not login yet
                if (string.IsNullOrEmpty(loginUser))
                {
                    filterContext.Result = SessionAuthorizeFilter.CreateLoginRequiredResult(filterContext);
                    return;
                }
            }
        }

        //參考
        //https://sdwh.dev/posts/2020/06/ASPNET-MVC-Auth-POC/
        public class UserAuthorizeAttribute : AuthorizeAttribute
        {
            private readonly Authority[] allowedroles;
            private const string UnauthorizedController = "AccessDenied";
            private const string UnauthorizedAction = "Index";
            public UserAuthorizeAttribute(params Authority[] roles)
            {
                this.allowedroles = roles;
            }
            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                bool authorize = false;
                var userId = httpContext?.Session?["user_id"]?.ToString();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return false;
                }

                var userAuth = httpContext.Session["user_auth"] as List<string>;

                if (userAuth != null)
                {
                    foreach (var role in allowedroles)
                    {
                        if (userAuth.IndexOf(role.ToString()) > -1) 
                            return true;
                    }
                }

                return authorize;
            }

            protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
            {
                var userId = filterContext?.HttpContext?.Session?["user_id"]?.ToString();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    filterContext.Result = SessionAuthorizeFilter.CreateLoginRequiredResult(filterContext);
                    return;
                }

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", UnauthorizedController },
                        { "action", UnauthorizedAction }
                    });
            }
        }
    }
}
