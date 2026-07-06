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
                if (!string.Equals(fileType, ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "副檔名需為 xlsx";
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
    }
}
