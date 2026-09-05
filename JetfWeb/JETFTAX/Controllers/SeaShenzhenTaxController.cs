using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.SeaShenzhenOriginal;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 新遞深圳稅單上傳控制器。
    /// </summary>
    public class SeaShenzhenTaxController : Controller
    {
        private readonly SeaShenzhenTaxUploadService _seaShenzhenTaxUploadService;

        public SeaShenzhenTaxController(SeaShenzhenTaxUploadService seaShenzhenTaxUploadService)
        {
            _seaShenzhenTaxUploadService = seaShenzhenTaxUploadService;
        }

        /// <summary>
        /// 上傳稅金資料頁。
        /// </summary>
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得報關行下拉選單。
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult GetTaxDataTypeOptions()
        {
            var list = new[]
                {
                    new { Value = string.Empty, Text = "請選擇" }
                }
                .Concat(Enum.GetValues(typeof(SeaShenzhenTaxDataType))
                .Cast<SeaShenzhenTaxDataType>()
                .Select(x => new
                {
                    Value = x.ToString(),
                    Text = x.ToDescription()
                }))
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 上傳稅金資料。
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult Upload(HttpPostedFileBase file, string dataDate, string dataType)
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

                SeaShenzhenTaxDataType dataTypeValue;
                if (string.IsNullOrWhiteSpace(dataType) || !EnumerableExtensions.TryParseCode<SeaShenzhenTaxDataType>(dataType, out dataTypeValue))
                {
                    responseModel.status = Status.error;
                    responseModel.msg = "請選擇報關行";
                    return Json(responseModel);
                }

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

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                var brokerName = dataTypeValue.ToDescription();
                if (string.IsNullOrWhiteSpace(fileNameWithoutExtension)
                    || fileNameWithoutExtension.IndexOf(brokerName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    responseModel.status = Status.error;
                    responseModel.msg = $"檔名需包含報關行「{brokerName}」，請確認報關行是否選擇正確";
                    return Json(responseModel);
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_tax_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                responseModel = _seaShenzhenTaxUploadService.Upload(filePath, dataDateValue, dataTypeValue);
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel);
        }

        /// <summary>
        /// 匯出轉檔異常明細 Excel。
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult ExportTransferExceptions(SeaShenzhenTaxTransferExceptionExportRequest request)
        {
            try
            {
                var fileBytes = _seaShenzhenTaxUploadService.ExportTransferExceptions(request?.Exceptions);
                var fileGuid = Guid.NewGuid().ToString();
                var fileName = $"新遞深圳稅金異常明細_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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
