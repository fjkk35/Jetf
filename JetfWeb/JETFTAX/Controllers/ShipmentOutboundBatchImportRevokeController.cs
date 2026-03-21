using Service.Models;
using Service.Services.ShipmentOutboundBatchImportRevoke;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class ShipmentOutboundBatchImportRevokeController : Controller
    {
        private readonly ShipmentOutboundBatchImportRevokeService _service;

        public ShipmentOutboundBatchImportRevokeController(ShipmentOutboundBatchImportRevokeService service)
        {
            _service = service;
        }

        // GET: ShipmentOutboundBatchImportRevoke
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 上傳貨件出庫取消批量資料
        /// </summary>
        [HttpPost]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            ResopnseModel resopnseModel = new ResopnseModel();
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

                resopnseModel = _service.RevokeOutbound(filePath);
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