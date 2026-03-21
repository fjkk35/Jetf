using JETFTAX.Models;
using JETFTAX.Models.BatchUploadProcess;
using JETFTAX.Models.ErrorOrderSend;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.BatchUploadProcess;
using Service.Services.ErrorOrderSend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ErrorOrderSendController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly ErrorOrderSendService _errorOrderSendService;

        private static readonly object _sendLock = new object();


        public ErrorOrderSendController(DropDownListService dropDownListService, ErrorOrderSendService errorOrderSendService)
        {
            _dropDownListService = dropDownListService;
            _errorOrderSendService = errorOrderSendService;
        }

        [UserAuthorize(Authority.ErrorOrderSend)]
        public ActionResult Index()
        {
            var vm = new ErrorOrderSendViewModel();
            vm.SmsMessageList = _dropDownListService.GetErrorOrderSmsMessages();
            vm.ErrorOrderSendList = _errorOrderSendService.GetErrorOrderSend();

            return View(vm);
        }


        [HttpPost]
        [UserAuthorize(Authority.ErrorOrderSend)]
        public ActionResult Upload(ErrorOrderSendViewModel vm, HttpPostedFileBase file) 
        {
            ResopnseModel resopnseModel = new ResopnseModel();
            try
            {
                string fileType, fileName, filePath;
                if (file != null)
                {
                    if (file.ContentLength > 0)
                    {
                        fileType = Path.GetExtension(file.FileName);
                        if (fileType != ".xlsx")
                        {
                            resopnseModel.status = Status.error;
                            resopnseModel.msg = "副檔名需為xlsx";
                        }

                        if (resopnseModel.status != Status.error)
                        {
                            fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                            filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                            file.SaveAs(filePath);
                            //寫入資料
                            resopnseModel = _errorOrderSendService.Upload(filePath, vm.SmsMessageId, Session["user_id"].ToString());
                        }
                    }
                }
                else
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "未選擇檔案";
                }
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize(Authority.ErrorOrderSend)]
        public ActionResult Send(int id)
        {
            lock (_sendLock)
            {
                var isSend = _errorOrderSendService.IsSend(id);
                if (isSend)
                {
                    return Json(new ResopnseModel()
                    {
                        status = Status.error,
                        msg = "已發送過訊息"
                    }, JsonRequestBehavior.AllowGet);
                }

                var response = _errorOrderSendService.Send(id, Session["user_id"].ToString());

                return Json(response, JsonRequestBehavior.AllowGet);
            }
        }


        [UserAuthorize(Authority.ErrorOrderSend)]
        public ActionResult ErrorOrderSendDetailExcel(int id)
        {
            var workbook = _errorOrderSendService.ErrorOrderSendDetailExcel(id);

            string handle = Guid.NewGuid().ToString();
            string fileName = $"{id}簡訊發送錯單_明細.xlsx";

            using (MemoryStream fileStream = new MemoryStream())
            {
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = "" }
            };
        }

        [UserAuthorize(Authority.ErrorOrderSend)]
        public ActionResult Delete(int id)
        {
            lock (_sendLock)
            {
                var isSend = _errorOrderSendService.IsSend(id);
                if (isSend)
                {
                    return Json(new ResopnseModel()
                    {
                        status = Status.error,
                        msg = "已發送過訊息，無法刪除"
                    }, JsonRequestBehavior.AllowGet);
                }

                var response = _errorOrderSendService.Delete(id, Session["user_id"].ToString());

                return Json(response, JsonRequestBehavior.AllowGet);
            }
        }
    }
}