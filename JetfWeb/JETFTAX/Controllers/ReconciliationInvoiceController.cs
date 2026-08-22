using Service.EnumTax;
using Service.Models;
using Service.Services.ReconciliationInvoice;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 代收銷帳作業控制器。
    /// </summary>
    public class ReconciliationInvoiceController : Controller
    {
        /// <summary>
        /// 代收銷帳作業服務。
        /// </summary>
        private readonly ReconciliationInvoiceService _reconciliationInvoiceService;

        /// <summary>
        /// 建立代收銷帳作業控制器。
        /// </summary>
        /// <param name="reconciliationService">代收銷帳作業服務。</param>
        public ReconciliationInvoiceController(ReconciliationInvoiceService reconciliationInvoiceService)
        {
            _reconciliationInvoiceService = reconciliationInvoiceService;
        }

        /// <summary>
        /// Upload invoice page.
        /// </summary>
        /// <returns>Upload invoice view.</returns>
        [UserAuthorize(Authority.ReconciliationUploadInvoice)]
        public ActionResult UploadInvoice()
        {
            return View();
        }

        /// <summary>
        /// 上傳發票 Excel。
        /// </summary>
        /// <param name="file">Excel 檔案。</param>
        /// <returns>上傳結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationUploadInvoice)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            var responseModel = new ResponseModel();

            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "未選擇檔案";
                    return Json(responseModel);
                }

                var fileType = Path.GetExtension(file.FileName);
                if (!string.Equals(fileType, ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "副檔名需為 xlsx";
                    return Json(responseModel);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                responseModel = _reconciliationInvoiceService.UploadInvoices(filePath);
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel);
        }

        /// <summary>
        /// 上傳發票 Excel 並刪除對應發票資料。
        /// </summary>
        /// <param name="file">Excel 檔案。</param>
        /// <returns>刪除結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationUploadInvoice)]
        public JsonResult Delete(HttpPostedFileBase file)
        {
            var responseModel = new ResponseModel();

            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "未選擇檔案";
                    return Json(responseModel);
                }

                var fileType = Path.GetExtension(file.FileName);
                if (!string.Equals(fileType, ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "副檔名需為 xlsx";
                    return Json(responseModel);
                }

                var uploadDirectory = Server.MapPath("~/UploadFIle");
                Directory.CreateDirectory(uploadDirectory);
                var fileName =
                    $"{Path.GetFileNameWithoutExtension(file.FileName)}_delete_" +
                    $"{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadDirectory, fileName);
                file.SaveAs(filePath);

                responseModel = _reconciliationInvoiceService.DeleteInvoices(filePath);
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel);
        }
    }
}
