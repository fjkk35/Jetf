using Service.EnumTax;
using Service.Extensions;
using Service.Services.ShortOverUnload;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
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

        /// <summary>取得短溢卸資料來源選項。</summary>
        /// <returns>列舉值及其顯示名稱。</returns>
        [HttpGet]
        public JsonResult GetDataSourceOptions()
        {
            var options = Enum.GetValues(typeof(ShortOverUnloadDataSource))
                .Cast<ShortOverUnloadDataSource>()
                .Select(source => new { Value = ((int)source).ToString(), Text = source.ToDescription() })
                .ToList();
            return Json(options, JsonRequestBehavior.AllowGet);
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
        /// <returns>產檔識別碼及未取得資料清單；失敗時回傳錯誤訊息。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Upload(HttpPostedFileBase file, ShortOverUnloadDataSource? format)
        {
            try
            {
                // 未選擇或無法繫結的資料來源不可進入匯入流程。
                if (!format.HasValue || !ModelState.IsValid)
                    throw new InvalidDataException("請選擇台中-短卸或台中-溢卸。");

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
                var result = await _service.GenerateAsync(file.InputStream, format.Value, importedAt);
                // 沿用共用下載流程，以 GUID 暫存報告，檔名直接回傳前端。
                var fileGuid = Guid.NewGuid().ToString();
                TempData[fileGuid] = result.Content;
                var fileName = $"{format.Value.ToDescription()}_{importedAt:yyyyMMddHHmmss}.xlsx";
                // 服務已依主號、分號合併缺少欄位，直接回傳給畫面。
                var response = Json(new { status = "success", fileGuid, fileName, missingData = result.MissingData });
                response.MaxJsonLength = int.MaxValue;
                return response;
            }
            catch (Exception ex)
            {
                // 保留服務提供的來源列號或查詢失敗原因，供畫面顯示。
                return Json(new { status = "error", msg = ex.Message });
            }
        }

    }
}
