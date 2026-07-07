using Service.EnumTax;
using Service.Models;
using Service.Services.ReconciliationAir;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 空快代收銷帳控制器。
    /// </summary>
    public class ReconciliationAirController : Controller
    {
        /// <summary>
        /// 空快代收銷帳服務。
        /// </summary>
        private readonly ReconciliationAirService _reconciliationAirService;

        /// <summary>
        /// 建立空快代收銷帳控制器。
        /// </summary>
        /// <param name="reconciliationAirService">空快代收銷帳服務。</param>
        public ReconciliationAirController(ReconciliationAirService reconciliationAirService)
        {
            _reconciliationAirService = reconciliationAirService;
        }

        /// <summary>
        /// 上傳空快資料頁面。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.ReconciliationAir)]
        public ActionResult UploadAir()
        {
            return View();
        }

        /// <summary>
        /// 上傳空快 Excel。
        /// </summary>
        /// <param name="file">Excel 檔案。</param>
        /// <param name="type">資料來源類型（FTZ / TACT）。</param>
        /// <returns>上傳結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationAir)]
        public JsonResult Upload(HttpPostedFileBase file, string type)
        {
            var responseModel = new ResponseModel();

            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "未選擇檔案";
                    return Json(responseModel);
                }

                var fileType = Path.GetExtension(file.FileName);
                if (!IsAllowedUploadFileType(type, fileType))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = GetUploadFileTypeErrorMessage(type);
                    return Json(responseModel);
                }

                if (string.IsNullOrWhiteSpace(type))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "請選擇資料來源";
                    return Json(responseModel);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                responseModel = _reconciliationAirService.UploadAir(filePath, type);
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel);
        }

        /// <summary>
        /// 判斷上傳檔案副檔名是否符合資料來源。
        /// </summary>
        /// <param name="type">資料來源類型。</param>
        /// <param name="fileType">副檔名。</param>
        /// <returns>是否允許上傳。</returns>
        private static bool IsAllowedUploadFileType(string type, string fileType)
        {
            if (string.Equals(type, "TACT", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(fileType, ".csv", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(type, "FTZ", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(fileType, ".xls", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fileType, ".xlsx", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// 取得上傳檔案副檔名錯誤訊息。
        /// </summary>
        /// <param name="type">資料來源類型。</param>
        /// <returns>錯誤訊息。</returns>
        private static string GetUploadFileTypeErrorMessage(string type)
        {
            if (string.Equals(type, "TACT", StringComparison.OrdinalIgnoreCase))
            {
                return "TACT-華儲上傳檔案副檔名需為 csv";
            }

            if (string.Equals(type, "FTZ", StringComparison.OrdinalIgnoreCase))
            {
                return "FTZ-遠雄上傳檔案副檔名需為 xls 或 xlsx";
            }

            return "資料來源需為 FTZ 或 TACT";
        }
    }
}
