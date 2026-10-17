using Service.EnumTax;
using Service.Services.ShortOverUnload;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    [UserAuthorize(Authority.ShortOverUnload)]
    public class ShortOverUnloadController : Controller
    {
        private readonly ShortOverUnloadService _service;

        /// <summary>
        /// 初始化短溢卸畫面與檔案處理控制器。
        /// </summary>
        /// <param name="service">短溢卸資料查詢及 Excel 產生服務。</param>
        public ShortOverUnloadController(ShortOverUnloadService service)
        {
            // 範例下載與匯入匯出共用同一個短溢卸服務。
            _service = service;
        }

        /// <summary>
        /// 顯示短溢卸上傳畫面。
        /// </summary>
        /// <returns>包含資料來源選單及檔案上傳欄位的畫面。</returns>
        public ActionResult Index()
        {
            // 存取權限由控制器層級的 UserAuthorize 統一驗證。
            return View();
        }

        /// <summary>
        /// 下載短卸匯入範例 Excel。
        /// </summary>
        /// <returns>供使用者填寫的四欄 Excel 範例檔案。</returns>
        [HttpGet]
        public ActionResult DownloadExample()
        {
            // 直接由服務在記憶體建立範例，不讀取實體範本。
            return File(_service.CreateExample(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "短卸匯入範例.xlsx");
        }

        /// <summary>
        /// 驗證上傳檔案並產生報告，回傳前端自動下載使用的檔案識別碼。
        /// </summary>
        /// <param name="file">使用者上傳的 Excel 檔案。</param>
        /// <param name="format">畫面選擇的短溢卸資料來源。</param>
        /// <returns>產檔結果及識別碼；失敗時回傳錯誤訊息。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Upload(HttpPostedFileBase file, ShortOverUnloadDataSource? format)
        {
            try
            {
                // 未選擇或無法繫結的資料來源不可進入匯入流程。
                if (!format.HasValue || !ModelState.IsValid)
                    throw new InvalidDataException("請選擇台中-短卸。");

                // 檔案存在且副檔名為支援的 Excel 格式後，才交由服務解析。
                if (file == null || file.ContentLength == 0)
                    throw new InvalidDataException("請選擇有資料的 Excel 檔案。");
                var extension = Path.GetExtension(file.FileName);
                if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("檔案需為 Excel（.xlsx 或 .xls）。");

                // 一次取得台北匯入時間，確保所有頁籤日期及檔名一致。
                var importedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));
                var bytes = _service.Generate(file.InputStream, format.Value, importedAt);
                // 報告暫存於目前使用者的 TempData，前端取得識別碼後立即下載。
                var handle = Guid.NewGuid().ToString("N");
                TempData["ShortOverUnload:" + handle] = bytes;
                TempData["ShortOverUnload:" + handle + ":name"] = $"台中-短卸_{importedAt:yyyyMMddHHmmss}.xlsx";
                return Json(new { status = "success", fileGuid = handle });
            }
            catch (Exception ex)
            {
                // 保留服務提供的來源列號或查詢失敗原因，供畫面顯示。
                return Json(new { status = "error", msg = ex.Message });
            }
        }

        /// <summary>
        /// 依產檔識別碼下載本次匯入的短卸報告。
        /// </summary>
        /// <param name="fileGuid">上傳成功時回傳的 N 格式 GUID。</param>
        /// <returns>Excel 報告；識別碼無效或暫存已失效時回傳找不到檔案。</returns>
        [HttpGet]
        public ActionResult Download(string fileGuid)
        {
            // 先驗證識別碼，再讀取本使用者的暫存報告及檔名。
            if (!Guid.TryParseExact(fileGuid, "N", out _))
                return HttpNotFound();
            var bytes = TempData["ShortOverUnload:" + fileGuid] as byte[];
            var name = TempData["ShortOverUnload:" + fileGuid + ":name"] as string;
            if (bytes == null)
                return HttpNotFound("匯出檔案已過期，請重新上傳。");
            // 回傳附件讓瀏覽器直接下載 Excel。
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name ?? "台中-短卸.xlsx");
        }
    }
}
