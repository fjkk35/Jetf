using Service.EnumTax;
using Service.Models;
using Service.Services.ShipmentOutboundBatchImport;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentOutboundBatchImportController : Controller
    {
        private readonly ShipmentOutboundBatchImportService _shipmentOutboundBatchImportService;

        public ShipmentOutboundBatchImportController(ShipmentOutboundBatchImportService shipmentOutboundBatchImportService)
        {
            _shipmentOutboundBatchImportService = shipmentOutboundBatchImportService;
        }

        // GET: ShipmentOutboundBatchImport
        [UserAuthorize(Authority.ShipmentOutboundBatchImport)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 上傳貨件出庫批量資料
        /// </summary>
        /// <param name="file">上傳的檔案</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentOutboundBatchImport)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            ResponseModel resopnseModel = new ResponseModel();
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "未選擇檔案";
                    return Json(resopnseModel);
                }

                var fileType = Path.GetExtension(file.FileName);
                if (fileType != ".xlsx")
                {
                    resopnseModel.status = Status.error;
                    resopnseModel.msg = "副檔名需為 xlsx";
                    return Json(resopnseModel);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                resopnseModel = _shipmentOutboundBatchImportService.UploadShipmentOutbound(filePath);
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }

            return Json(resopnseModel);
        }
    }
}