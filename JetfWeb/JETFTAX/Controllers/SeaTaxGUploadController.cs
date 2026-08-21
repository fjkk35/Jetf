using Service.Models;
using Service.EnumTax;
using Service.Services.SeaTaxGUpload;
using System;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// G 類海運稅金資料上傳。
    /// </summary>
    public class SeaTaxGUploadController : Controller
    {
        private readonly SeaTaxGUploadService _seaTaxGUploadService;

        /// <summary>
        /// 建立 G 類海運稅金資料上傳控制器。
        /// </summary>
        /// <param name="seaTaxGUploadService">G 類海運稅金資料上傳服務。</param>
        public SeaTaxGUploadController(SeaTaxGUploadService seaTaxGUploadService)
        {
            _seaTaxGUploadService = seaTaxGUploadService;
        }

        /// <summary>
        /// G 類海運稅金資料上傳頁面。
        /// </summary>
        [UserAuthorize(Authority.UploadSeaTaxG)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 上傳 G 類海運稅金 Excel。
        /// </summary>
        /// <param name="file">Excel 檔案。</param>
        /// <param name="date">資料日期。</param>
        /// <returns>處理結果。</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize(Authority.UploadSeaTaxG)]
        public JsonResult UploadFile(HttpPostedFileBase file, string date)
        {
            try
            {
                DateTime uploadDate;
                if (!DateTime.TryParseExact(
                    date,
                    new[] { "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out uploadDate))
                {
                    return Json(new ResponseModel("請選擇正確的日期"));
                }

                if (file == null || file.ContentLength <= 0)
                {
                    return Json(new ResponseModel("未選擇檔案"));
                }

                if (!string.Equals(
                    Path.GetExtension(file.FileName),
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new ResponseModel("副檔名需為 xlsx"));
                }

                // 保留原始上傳檔案，檔名加入時間避免同名檔案互相覆蓋。
                var savedFileName =
                    $"{Path.GetFileNameWithoutExtension(file.FileName)}_" +
                    $"{DateTime.Now:yyyyMMddHHmmssfff}.xlsx";
                var uploadDirectory = Server.MapPath("~/UploadFIle");
                Directory.CreateDirectory(uploadDirectory);
                var savedFilePath = Path.Combine(uploadDirectory, savedFileName);
                file.SaveAs(savedFilePath);

                ResponseModel response;
                using (var fileStream = System.IO.File.OpenRead(savedFilePath))
                {
                    response = _seaTaxGUploadService.Upload(
                        uploadDate.ToString("yyyyMMdd"),
                        fileStream);
                }
                return Json(response);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}
