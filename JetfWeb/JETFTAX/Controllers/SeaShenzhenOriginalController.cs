using Service.EnumTax;
using Service.Models;
using Service.Services.SeaShenzhenOriginal;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaShenzhenOriginalController : Controller
    {
        private readonly SeaShenzhenOriginalService _seaShenzhenOriginalService;

        public SeaShenzhenOriginalController(SeaShenzhenOriginalService seaShenzhenOriginalService)
        {
            _seaShenzhenOriginalService = seaShenzhenOriginalService;
        }

        /// <summary>
        /// 上傳託運資料。
        /// </summary>
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 上傳託運資料。
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult Upload(HttpPostedFileBase file, string dataDate)
        {
            var responseModel = new ResponseModel();
            try
            {
                DateTime dataDateValue;
                if (string.IsNullOrWhiteSpace(dataDate) || !DateTime.TryParse(dataDate, out dataDateValue))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "請選擇資料日期";
                    return Json(responseModel);
                }

                if (file == null || file.ContentLength == 0)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "未選擇檔案";
                    return Json(responseModel);
                }

                var fileType = Path.GetExtension(file.FileName);
                if (fileType != ".xlsx")
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "副檔名需為 xlsx";
                    return Json(responseModel);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                responseModel = _seaShenzhenOriginalService.Upload(filePath, dataDateValue);
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
