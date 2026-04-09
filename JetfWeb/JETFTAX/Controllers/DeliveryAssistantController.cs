using Service.EnumTax;
using Service.Models;
using Service.Services.DeliveryAssistant;
using Service.Services.DeliveryAssistant.Domain;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class DeliveryAssistantController : Controller
    {
        private readonly DeliveryAssistantService _deliveryAssistantService;

        public DeliveryAssistantController(DeliveryAssistantService deliveryAssistantService)
        {
            _deliveryAssistantService = deliveryAssistantService;
        }

        // GET: DeliveryAssistant
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得作業地區清單
        /// </summary>
        [HttpGet]
        public JsonResult GetDataTypeList()
        {
            try
            {
                var list = _deliveryAssistantService.GetDataTypeList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得派件公司清單
        /// </summary>
        [HttpGet]
        public JsonResult GetTransList()
        {
            try
            {
                var list = _deliveryAssistantService.GetTransList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        [HttpPost]
        public ActionResult ExportExcel(DeliveryAssistantRequest request)
        {
            try
            {
                var fileBytes = _deliveryAssistantService.ExportExcel(request);

                string startDate = DateTime.TryParse(request.StartDate, out var sd) ? sd.ToString("yyyyMMdd") : "";
                string endDate = DateTime.TryParse(request.EndDate, out var ed) ? ed.ToString("yyyyMMdd") : "";
                string fileName = $"派送助理_{startDate}_{endDate}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 上傳 Excel 並呼叫外部 API
        /// </summary>
        [HttpPost]
        public JsonResult UploadOrderInfo(HttpPostedFileBase file)
        {
            ResponseModel result = new ResponseModel();
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    result.status = Status.error;
                    result.msg = "未選擇檔案";
                    return Json(result);
                }

                if (Path.GetExtension(file.FileName).ToLower() != ".xlsx")
                {
                    result.status = Status.error;
                    result.msg = "副檔名需為 xlsx";
                    return Json(result);
                }

                string fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                string filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                result = _deliveryAssistantService.UploadOrderInfo(filePath);
            }
            catch (Exception ex)
            {
                result.status = Status.error;
                result.msg = ex.Message;
            }

            return Json(result);
        }
    }
}