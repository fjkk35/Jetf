using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.SeaShenzhenOriginal;
using System;
using System.IO;
using System.Linq;
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
        /// 上傳託運資料。
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
                if (fileType != ".xlsx")
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

                var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(Server.MapPath("~/UploadFIle"), fileName);
                file.SaveAs(filePath);

                responseModel = _seaShenzhenOriginalService.Upload(filePath, dataDateValue, dataTypeValue);
            }
            catch (Exception ex)
            {
                responseModel.status = Status.error;
                responseModel.msg = ex.Message;
            }

            return Json(responseModel);
        }

        /// <summary>
        /// 下載上傳託運資料範例。
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult DownloadTemplate()
        {
            var fileBytes = _seaShenzhenOriginalService.ExportTemplate();
            return File(fileBytes, "application/octet-stream", "新遞託運資料_範例.xlsx");
        }
    }
}
