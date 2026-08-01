using Service.EnumTax;
using Service.Models;
using Service.Services.ReconciliationTaxCustomerAdjustment;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 稅金客戶調整控制器。
    /// </summary>
    public sealed class ReconciliationTaxCustomerAdjustmentController : Controller
    {
        private readonly ReconciliationTaxCustomerAdjustmentService _service;

        /// <summary>
        /// 建立稅金客戶調整控制器。
        /// </summary>
        /// <param name="service">稅金客戶調整服務。</param>
        public ReconciliationTaxCustomerAdjustmentController(
            ReconciliationTaxCustomerAdjustmentService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示稅金客戶調整頁面。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.ReconciliationTaxCustomerAdjustment)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 下載稅金客戶調整範例檔。
        /// </summary>
        /// <returns>Excel 範例檔。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationTaxCustomerAdjustment)]
        public ActionResult DownloadTemplate()
        {
            var fileBytes = _service.ExportTemplate();
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "稅金客戶調整_範例.xlsx");
        }

        /// <summary>
        /// 上傳稅金客戶調整 Excel 檔。
        /// </summary>
        /// <param name="file">Excel 檔案。</param>
        /// <returns>上傳結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationTaxCustomerAdjustment)]
        public JsonResult Upload(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return Json(new ResponseModel("請選擇上傳檔案。"));
            }

            if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
            {
                return Json(new ResponseModel("檔案格式必須為 .xlsx。"));
            }

            var fileName = string.Format(
                "{0}_{1}{2}",
                Path.GetFileNameWithoutExtension(file.FileName),
                DateTime.Now.ToString("yyyyMMddHHmmss"),
                Path.GetExtension(file.FileName));
            var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
            file.SaveAs(filePath);

            return Json(_service.Upload(filePath));
        }
    }
}
