using JETFTAX.Models;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using static JETFTAX.Models.AccountViewModel;

namespace JETFTAX.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;

        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        // GET: Account
        public ActionResult Login()
        {
            return View();
        }

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
                var authority = _accountService.GetAuthority(model.Id);

                Session["user_id"] = model.Id;
                Session["user_name"] = model.Name;
                Session["user_partner"] = authority.Item1;
                Session["user_auth"] = authority.Item2;
            }
            //記錄LOG
            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }


        public ActionResult LogOff()
        {
            //清除Session
            Session.Remove("user_id");
            Session.Remove("user_name");
            Session.Remove("user_auth");
            return RedirectToAction("Login", "Account");
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
                    var redirectUrl = "~/Account/Login";
                    if (!filterContext.HttpContext.Request.IsAjaxRequest())
                    {
                        filterContext.Result = new RedirectResult(redirectUrl);
                    }
                    else
                    {
                        filterContext.Result = new JsonResult
                        {
                            Data = new
                            {
                                Success = false,
                                Message = string.Empty,
                                Redirect = redirectUrl,

                                //recordsTotal = 0,
                                //recordsFiltered = 0,
                                data = "[]",
                            },
                            JsonRequestBehavior= JsonRequestBehavior.AllowGet
                        };
                    }
                    return;
                }
            }
        }

        //參考
        //https://sdwh.dev/posts/2020/06/ASPNET-MVC-Auth-POC/
        public class UserAuthorizeAttribute : AuthorizeAttribute
        {
            private readonly Authority[] allowedroles;
            private string controller = "Cargo";
            private string action = "SearchCargo";
            public UserAuthorizeAttribute(params Authority[] roles)
            {
                this.allowedroles = roles;
            }
            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                bool authorize = false;
                var userAuth = httpContext.Session["user_auth"] as List<string>;

                if (userAuth != null)
                {
                    foreach (var role in allowedroles)
                    {
                        if (userAuth.IndexOf(role.ToString()) > -1) 
                            return true;
                    }
                }

                if (!authorize && userAuth != null && userAuth.IndexOf(Authority.SearchCargo.ToString()) < 0 )
                {
                    controller = "Home";
                    action = "Index";
                }

                return authorize;
            }

            protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
            {
                filterContext.Result = new RedirectToRouteResult(
                   new RouteValueDictionary
                   {
                        { "controller", controller },
                        { "action", action }
                   });
            }
        }
    }
}