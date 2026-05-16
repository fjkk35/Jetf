using JETFTAX.Models;
using Service.EnumTax;
using Service.Models;
using Service.Services.DownloadSeaNew;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class DownloadSeaNewController : Controller
    {
        private readonly DownloadSeaNewService _downloadSeaNewService;

        /// <summary>
        /// 初始化海運新版下載控制器。
        /// </summary>
        /// <param name="downloadSeaNewService">海運新版下載服務。</param>
        public DownloadSeaNewController(DownloadSeaNewService downloadSeaNewService)
        {
            _downloadSeaNewService = downloadSeaNewService;
        }

        /// <summary>
        /// 物流代收檔下載-海運(新) 首頁。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 下載海運一般代收檔。
        /// </summary>
        /// <param name="vm">查詢條件。</param>
        /// <returns>下載結果。</returns>
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaExcel(DownloadSeaViewModel vm)
        {
            var exportResult = _downloadSeaNewService.GetNormalExport(vm.date, vm.taxType);
            return Json(CreateDownloadResponse(exportResult), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 下載海運無客戶代收檔。
        /// </summary>
        /// <param name="vm">查詢條件。</param>
        /// <returns>下載結果。</returns>
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaErrorExcel(DownloadSeaViewModel vm)
        {
            var exportResult = _downloadSeaNewService.GetErrorExport(vm.date, vm.taxType);
            return Json(CreateDownloadResponse(exportResult), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 下載海運特殊客戶 D 檔。
        /// </summary>
        /// <param name="vm">查詢條件。</param>
        /// <returns>下載結果。</returns>
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaSpecialDExcel(DownloadSeaViewModel vm)
        {
            var exportResult = _downloadSeaNewService.GetSpecialDExport(vm.date, vm.taxType);
            return Json(CreateDownloadResponse(exportResult), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 下載海運特殊客戶 C 檔。
        /// </summary>
        /// <param name="vm">查詢條件。</param>
        /// <returns>下載結果。</returns>
        [UserAuthorize(Authority.DownloadSeaTax)]
        public ActionResult SeaSpecialCExcel(DownloadSeaViewModel vm)
        {
            var exportResult = _downloadSeaNewService.GetSpecialCExport(vm.date, vm.taxType);
            return Json(CreateDownloadResponse(exportResult), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 將 service 準備好的檔案內容掛到 TempData，供前端後續下載。
        /// </summary>
        /// <param name="exportResult">service 回傳的匯出結果。</param>
        /// <returns>前端下載用的回應資料。</returns>
        private object CreateDownloadResponse(DownloadSeaNewExportResult exportResult)
        {
            var handle = Guid.NewGuid().ToString();

            try
            {
                if (exportResult.status == Status.success &&
                    !string.IsNullOrEmpty(exportResult.FileName) &&
                    exportResult.FileBytes != null &&
                    exportResult.FileBytes.Length > 0)
                {
                    TempData[handle] = exportResult.FileBytes;
                }
            }
            catch (Exception ex)
            {
                exportResult.msg = ex.Message;
            }

            return new { fileGuid = handle, fileName = exportResult.FileName, msg = exportResult.msg };
        }
    }
}
