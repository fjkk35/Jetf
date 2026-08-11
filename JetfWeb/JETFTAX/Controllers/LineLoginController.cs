using JETFTAX.Models;
using JETFTAX.Models.LineLogin;
using Newtonsoft.Json;
using Service.Models;
using Service.Services;
using Service.Services.LineLogin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class LineLoginController : Controller
    {
        private readonly LineLoginService _lineLoginService;

        public LineLoginController(LineLoginService lineLoginService)
        {
            _lineLoginService = lineLoginService;
        }

        //https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id=2006592376&redirect_uri=https://localhost:44347/LineLogin/PhoneBind&state=1234567489&scope=profile%20openid%20email



        /// <summary>
        /// LINE 電話綁定
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult PhoneBind(string code, string state)
        {
            var tokenAsync = _lineLoginService.GetAccessToken(code);

            var userProfile = _lineLoginService.GetUserProfile(tokenAsync.AccessToken, tokenAsync.IdToken);

            //更新Line用戶資料
            _lineLoginService.UpsertLineUserProfile(userProfile);

            var phone = _lineLoginService.GetPhone(userProfile.UserId);

            var vm = new LineUserProfileViewModel
            {
                UserId = userProfile.UserId,
                DisplayName = userProfile.DisplayName,
                Phone = phone,
                IsBind = !string.IsNullOrEmpty(phone)
            };

            return View(vm);
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult PhoneBind(LineUserProfileViewModel vm)
        {
            try
            {
                var resopnse = _lineLoginService.UpdatePhone(vm.UserId, vm.Phone);

                return Json(resopnse, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel("綁定手機失敗，請聯絡客服"), JsonRequestBehavior.AllowGet);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Privacy() 
        { 
            return View();
        }
    }
}
