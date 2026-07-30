using Service.EnumTax;
using Service.Models;
using Service.Services.ReconciliationTaxDlvInvAdjustment;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 稅金物流貨號調整控制器。
    /// </summary>
    public sealed class ReconciliationTaxDlvInvAdjustmentController : Controller
    {
        private readonly ReconciliationTaxDlvInvAdjustmentService
            _reconciliationTaxDlvInvAdjustmentService;

        /// <summary>
        /// 建立稅金物流貨號調整控制器。
        /// </summary>
        /// <param name="reconciliationTaxDlvInvAdjustmentService">
        /// 稅金物流貨號調整服務。
        /// </param>
        public ReconciliationTaxDlvInvAdjustmentController(
            ReconciliationTaxDlvInvAdjustmentService
                reconciliationTaxDlvInvAdjustmentService)
        {
            _reconciliationTaxDlvInvAdjustmentService =
                reconciliationTaxDlvInvAdjustmentService;
        }

        /// <summary>
        /// 稅金物流貨號調整頁面。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.ReconciliationTaxDlvInvAdjustment)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 下載稅金物流貨號調整 Excel 範例。
        /// </summary>
        /// <returns>Excel 範例檔案。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationTaxDlvInvAdjustment)]
        public ActionResult DownloadTemplate()
        {
            var fileBytes = _reconciliationTaxDlvInvAdjustmentService.ExportTemplate();
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "稅金物流貨號調整_範例.xlsx");
        }

        /// <summary>
        /// 上傳稅金物流貨號調整 Excel。
        /// </summary>
        /// <param name="file">Excel 檔案。</param>
        /// <returns>上傳結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationTaxDlvInvAdjustment)]
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

            var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_" +
                $"{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
            file.SaveAs(filePath);

            var response = _reconciliationTaxDlvInvAdjustmentService.Upload(
                filePath);
            return Json(response);
        }
    }
}
