using Service.EnumTax;
using Service.Services.SeaShenzhenOriginal;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 新遞物流代收檔下載控制器。
    /// </summary>
    public class SeaShenzhenFeeDownloadController : Controller
    {
        private readonly SeaShenzhenFeeDownloadService _seaShenzhenFeeDownloadService;

        public SeaShenzhenFeeDownloadController(SeaShenzhenFeeDownloadService seaShenzhenFeeDownloadService)
        {
            _seaShenzhenFeeDownloadService = seaShenzhenFeeDownloadService;
        }

        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 依資料日期產生物流代收檔 Excel，並回傳暫存下載資訊。
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult ExportExcel(SeaShenzhenFeeTransferRequest request)
        {
            try
            {
                var fileBytes = _seaShenzhenFeeDownloadService.ExportCollectibleExcel(request);
                var fileGuid = Guid.NewGuid().ToString();
                var fileName = $"新遞物流代收檔_{request?.DataDate}.xlsx";

                TempData[fileGuid] = fileBytes;

                return Json(new
                {
                    fileGuid,
                    fileName,
                    msg = string.Empty
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    fileGuid = string.Empty,
                    fileName = string.Empty,
                    msg = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}