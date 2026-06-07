using Service.EnumTax;
using Service.Models;
using Service.Services.SeaShenzhenOriginal;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 新遞代收金額人工調整控制器。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodController : Controller
    {
        private readonly SeaShenzhenFeeManualToDlvCodService _seaShenzhenFeeManualToDlvCodService;

        public SeaShenzhenFeeManualToDlvCodController(SeaShenzhenFeeManualToDlvCodService seaShenzhenFeeManualToDlvCodService)
        {
            _seaShenzhenFeeManualToDlvCodService = seaShenzhenFeeManualToDlvCodService;
        }

        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 依條件查詢人工調整資料。
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult SearchData(SeaShenzhenFeeManualToDlvCodQueryRequest request)
        {
            try
            {
                var result = _seaShenzhenFeeManualToDlvCodService.GetData(request);

                return Json(new
                {
                    Data = result.Data,
                    TotalCount = result.TotalCount
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult Upload(HttpPostedFileBase file)
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
                if (fileType != ".xlsx")
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "副檔名需為 xlsx";
                    return Json(responseModel);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                responseModel = _seaShenzhenFeeManualToDlvCodService.Upload(filePath);
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel);
        }

        [HttpGet]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult DownloadTemplate()
        {
            var fileBytes = _seaShenzhenFeeManualToDlvCodService.ExportTemplate();
            return File(fileBytes, "application/octet-stream", "新遞代收金額人工調整_範例.xlsx");
        }
    }
}
