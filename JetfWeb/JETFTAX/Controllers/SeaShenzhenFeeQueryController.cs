using Service.EnumTax;
using Service.Extensions;
using Service.Services.SeaShenzhenOriginal;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Linq;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 新遞稅金資料查詢控制器。
    /// </summary>
    public class SeaShenzhenFeeQueryController : Controller
    {
        private readonly SeaShenzhenFeeQueryService _seaShenzhenFeeQueryService;

        public SeaShenzhenFeeQueryController(SeaShenzhenFeeQueryService seaShenzhenFeeQueryService)
        {
            _seaShenzhenFeeQueryService = seaShenzhenFeeQueryService;
        }

        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult SearchData(SeaShenzhenFeeQueryRequest request)
        {
            try
            {
                var result = _seaShenzhenFeeQueryService.GetData(request);
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
        public JsonResult ExportExcel(SeaShenzhenFeeQueryRequest request)
        {
            try
            {
                var fileBytes = _seaShenzhenFeeQueryService.ExportExcel(request);
                var fileGuid = Guid.NewGuid().ToString();
                var fileName = BuildFileName(request);

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

        [HttpGet]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult GetTaxPaymentOptions()
        {
            var list = new[]
            {
                new { Value = string.Empty, Text = "全部" }
            }.Concat(Enum.GetValues(typeof(ShenzhenTaxPayment))
                .Cast<ShenzhenTaxPayment>()
                .Select(item => new
                {
                    Value = item.ToString(),
                    Text = item + "-" + item.ToDescription()
                }))
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult GetDataTypeOptions()
        {
            var list = new[]
            {
                new { Value = string.Empty, Text = "全部" }
            }.Concat(Enum.GetValues(typeof(SeaShenzhenTaxDataType))
                .Cast<SeaShenzhenTaxDataType>()
                .Select(item => new
                {
                    Value = item.ToString(),
                    Text = item.ToDescription()
                }))
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        private static string BuildFileName(SeaShenzhenFeeQueryRequest request)
        {
            var startDate = NormalizeDateLabel(request?.DataDateStart);
            var endDate = NormalizeDateLabel(request?.DataDateEnd);

            if (!string.IsNullOrWhiteSpace(startDate) || !string.IsNullOrWhiteSpace(endDate))
            {
                return $"新遞稅金資料查詢_{startDate}_{endDate}.xlsx";
            }

            return "新遞稅金資料查詢.xlsx";
        }

        private static string NormalizeDateLabel(string value)
        {
            DateTime dateValue;
            if (DateTime.TryParse(value, out dateValue))
            {
                return dateValue.ToString("yyyyMMdd");
            }

            var trimmedValue = (value ?? string.Empty).Trim().Replace("-", string.Empty).Replace("/", string.Empty);
            return trimmedValue;
        }
    }
}