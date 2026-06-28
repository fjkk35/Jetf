using System.Net;
using System.Web.Mvc;

namespace JETFTAX.Infrastructure
{
    /// <summary>
    /// Ensures MVC actions have a logged-in user session.
    /// </summary>
    public sealed class SessionAuthorizeFilter : AuthorizeAttribute
    {
        private const string LoginUrl = "~/Account/Login";

        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            var userId = httpContext?.Session?["user_id"]?.ToString();
            return !string.IsNullOrWhiteSpace(userId);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = CreateLoginRequiredResult(filterContext);
        }

        public static ActionResult CreateLoginRequiredResult(AuthorizationContext filterContext)
        {
            var loginUrl = ResolveLoginUrl(filterContext);

            if (filterContext?.HttpContext?.Request?.IsAjaxRequest() == true)
            {
                var response = filterContext.HttpContext.Response;
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.TrySkipIisCustomErrors = true;
                response.AddHeader("X-Login-Redirect", loginUrl);

                return new JsonResult
                {
                    Data = new
                    {
                        Success = false,
                        Message = "登入逾時，請重新登入。",
                        Redirect = loginUrl,
                        data = "[]",
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }

            return new RedirectResult(loginUrl);
        }

        private static string ResolveLoginUrl(AuthorizationContext filterContext)
        {
            if (filterContext?.RequestContext == null)
            {
                return LoginUrl;
            }

            return new UrlHelper(filterContext.RequestContext).Content(LoginUrl);
        }
    }
}
