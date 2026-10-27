using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.FlightArrivalUpload;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// A03、B6F 與班機派件送達檔案上傳控制器。
    /// </summary>
    [UserAuthorize(Authority.UploadFlightArrival)]
    public sealed class FlightArrivalUploadController : Controller
    {
        private readonly FlightArrivalUploadService _service;

        /// <summary>
        /// 建立檔案上傳控制器。
        /// </summary>
        /// <param name="service">檔案上傳服務。</param>
        public FlightArrivalUploadController(FlightArrivalUploadService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示檔案上傳頁面。
        /// </summary>
        /// <returns>上傳頁面。</returns>
        [HttpGet]
        public ActionResult UploadFile()
        {
            return View();
        }

        /// <summary>
        /// 保存並匯入 A03、B6F 與班機派件送達資料。
        /// </summary>
        /// <param name="file">上傳的 xlsx 檔案。</param>
        /// <returns>匯入結果。</returns>
        [HttpPost]
        public JsonResult UploadFile(HttpPostedFileBase file)
        {
            var response = new ResponseModel();
            try
            {
                if (file == null || file.ContentLength == 0)
                {
                    return Json(new ResponseModel("未選擇檔案"));
                }

                if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new ResponseModel("副檔名需為xlsx"));
                }

                var uploadDirectory = Server.MapPath("~/UploadFIle");
                Directory.CreateDirectory(uploadDirectory);
                var originalName = Path.GetFileNameWithoutExtension(file.FileName);
                var fileName = $"{originalName}_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.xlsx";
                var filePath = Path.Combine(uploadDirectory, fileName);
                file.SaveAs(filePath);

                response = _service.UploadFile(filePath, UserContextService.GetUserId());
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.status = Status.error;
                response.msg = ex.Message;
            }

            return Json(response);
        }
    }
}
