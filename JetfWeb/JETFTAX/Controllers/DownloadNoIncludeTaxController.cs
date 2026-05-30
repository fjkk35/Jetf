using JETFTAX.Models;
using Service.EnumTax;
using Service.Models;
using Service.Services.DownloadNoIncludeTax;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class DownloadNoIncludeTaxController : Controller
    {
        private readonly DownloadNoIncludeTaxService _downloadNoIncludeTaxService;

        public DownloadNoIncludeTaxController(DownloadNoIncludeTaxService downloadNoIncludeTaxService)
        {
            _downloadNoIncludeTaxService = downloadNoIncludeTaxService;
        }

        [UserAuthorize(Authority.DownloadEtlWarehouse)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.DownloadEtlWarehouse)]
        public JsonResult NoIncludeTaxExcel(DownloadNoIncludeTaxViewModel viewModel)
        {
            try
            {
                // step1: 產生 Excel 內容；失敗時只回傳訊息，不建立下載 handle。
                var exportResult = _downloadNoIncludeTaxService.Export(
                    viewModel?.source,
                    viewModel?.sDate,
                    viewModel?.eDate);

                if (exportResult.status != Status.success ||
                    exportResult.FileBytes == null ||
                    exportResult.FileBytes.Length == 0)
                {
                    return Json(new
                    {
                        fileGuid = string.Empty,
                        fileName = string.Empty,
                        msg = exportResult.msg
                    });
                }

                // step2: 暫存檔案內容，前端再使用既有 DownloadFile action 取得實體檔案。
                var handle = Guid.NewGuid().ToString();
                TempData[handle] = exportResult.FileBytes;

                // step3: 回傳一次性 handle，避免將大型 Excel 內容塞進 JSON response。
                return Json(new
                {
                    fileGuid = handle,
                    fileName = exportResult.FileName,
                    msg = exportResult.msg
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
