using JETFTAX.Models.ErrorOrderSendCustomer;
using Service.Models.ErrorOrderSend;
using Service.Models;
using Service.Services.ErrorOrderSendCustomer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JETFTAX.Models.ErrorOrderSmsMessage;
using Service.Services.ErrorOrderSmsMessage;
using Service.Models.ErrorOrderSmsMessage;

namespace JETFTAX.Controllers
{
    public class ErrorOrderSmsMessageController : Controller
    {
        private readonly ErrorOrderSmsMessageService _errorOrderSmsMessageService;

        public ErrorOrderSmsMessageController(ErrorOrderSmsMessageService errorOrderSmsMessageService)
        {
            _errorOrderSmsMessageService = errorOrderSmsMessageService;
        }

        // GET: ErrorOrderSmsMessage
        public ActionResult Index()
        {
            var vm = new ErrorOrderSmsMessageViewModel()
            {
                List = _errorOrderSmsMessageService.GetErrorOrderSmsMessage()
            };

            return View(vm);
        }

        public ActionResult Create(ErrorOrderSmsMessageModel model)
        {
            if (string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.Content))
                return Json(new ResponseModel("請輸入簡訊名稱、內容"));

            if (model.Content.Contains("＜平台＞") == false)
            {
                return Json(new ResponseModel("簡訊內容沒有輸入：＜平台＞"));
            }

            if (model.Content.Contains("＜分提單號＞") == false)
            {
                return Json(new ResponseModel("簡訊內容沒有輸入：＜分提單號＞"));
            }

            var userId = Session["user_id"].ToString();

            var result = _errorOrderSmsMessageService.Create(model, userId);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Update(ErrorOrderSmsMessageModel model)
        {
            if (string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.Content))
                return Json(new ResponseModel("請輸入簡訊名稱、內容"));

            if (model.Content.Contains("＜平台＞") == false)
            {
                return Json(new ResponseModel("簡訊內容沒有輸入：＜平台＞"));
            }

            if (model.Content.Contains("＜分提單號＞") == false)
            {
                return Json(new ResponseModel("簡訊內容沒有輸入：＜分提單號＞"));
            }

            var userId = Session["user_id"].ToString();

            var result = _errorOrderSmsMessageService.Update(model, userId);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(int id)
        {
            var vm = new ErrorOrderSmsMessageDetailViewModel()
            {
                SmsMessage = _errorOrderSmsMessageService.GetDetail(id)
            };

            return PartialView(vm);
        }


        public ActionResult Delete(int id)
        {
            var result = _errorOrderSmsMessageService.Delete(id);

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}