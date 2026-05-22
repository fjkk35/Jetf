using Service.EnumTax;
using Service.Models;
using Service.Services.ShipmentInboundReturnImport;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentInboundReturnImportController : Controller
    {
        private readonly ShipmentInboundReturnImportService _shipmentInboundReturnImportService;

        public ShipmentInboundReturnImportController(ShipmentInboundReturnImportService shipmentInboundReturnImportService)
        {
            _shipmentInboundReturnImportService = shipmentInboundReturnImportService;
        }

        [UserAuthorize(Authority.ShipmentInboundBatchImport)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundBatchImport)]
        public JsonResult Upload(HttpPostedFileBase file, string dataType)
        {
            ResponseModel resopnseModel = new ResponseModel();
            string filePath = null;
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

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                resopnseModel = _shipmentInboundReturnImportService.UploadShipmentInbound(filePath, dataType);
            }
            catch (Exception ex)
            {
                resopnseModel.status = Status.error;
                resopnseModel.msg = ex.Message;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            return Json(resopnseModel);
        }
    }
}