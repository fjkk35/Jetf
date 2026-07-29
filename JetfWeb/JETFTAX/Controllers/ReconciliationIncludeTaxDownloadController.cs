using Service.EnumTax;
using Service.Models;
using Service.Services.ReconciliationIncludeTaxDownload;
using Service.Services.ReconciliationIncludeTaxDownload.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 包稅客戶明細下載控制器。
    /// </summary>
    public sealed class ReconciliationIncludeTaxDownloadController : Controller
    {
        private readonly ReconciliationIncludeTaxDownloadService _service;

        /// <summary>
        /// 建立包稅客戶明細下載控制器。
        /// </summary>
        /// <param name="service">包稅客戶明細下載服務。</param>
        public ReconciliationIncludeTaxDownloadController(
            ReconciliationIncludeTaxDownloadService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示包稅客戶明細下載頁面。
        /// </summary>
        /// <returns>下載頁面。</returns>
        [UserAuthorize(Authority.ReconciliationIncludeTaxDownload)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得包稅客戶匯出格式。
        /// </summary>
        /// <returns>格式清單。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationIncludeTaxDownload)]
        public JsonResult GetFormats()
        {
            try
            {
                return Json(new ResponseModel(_service.GetFormats()), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得海運、空運客戶及客戶群組選項。
        /// </summary>
        /// <returns>客戶選擇資料。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationIncludeTaxDownload)]
        public JsonResult GetCustomerSelectionOptions()
        {
            try
            {
                return Json(
                    new ResponseModel(_service.GetCustomerSelectionOptions()),
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 依條件產生包稅客戶明細 Excel。
        /// </summary>
        /// <param name="request">下載查詢條件。</param>
        /// <returns>一次性下載檔案識別資訊。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationIncludeTaxDownload)]
        public JsonResult ExportExcel(ReconciliationIncludeTaxDownloadRequest request)
        {
            try
            {
                var exportResult = _service.Export(request);
                var handle = Guid.NewGuid().ToString();
                TempData[handle] = exportResult.FileBytes;
                return Json(new
                {
                    fileGuid = handle,
                    fileName = exportResult.FileName,
                    msg = string.Empty
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    fileGuid = string.Empty,
                    fileName = string.Empty,
                    msg = ex.Message
                });
            }
        }
    }
}
