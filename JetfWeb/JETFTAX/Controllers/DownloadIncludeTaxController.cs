using Service.EnumTax;
using Service.Models;
using Service.Services.DownloadIncludeTax;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 稅金總表及明細表頁面控制器。
    /// </summary>
    public sealed class DownloadIncludeTaxController : Controller
    {
        private readonly DownloadIncludeTaxService _service;

        /// <summary>
        /// 建立控制器。
        /// </summary>
        /// <param name="service">稅金總表匯出服務。</param>
        public DownloadIncludeTaxController(DownloadIncludeTaxService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示稅金總表及明細表頁面。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.DownloadTaxReport)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 匯出稅金總表及明細表 Excel。
        /// </summary>
        /// <param name="request">匯出條件。</param>
        /// <returns>下載資訊。</returns>
        [HttpPost]
        [UserAuthorize(Authority.DownloadTaxReport)]
        public JsonResult ExportExcel(DownloadIncludeTaxRequest request)
        {
            try
            {
                var result = _service.Export(request);
                var fileGuid = Guid.NewGuid().ToString();
                TempData[fileGuid] = result.FileBytes;
                return Json(new
                {
                    fileGuid,
                    fileName = result.FileName,
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
