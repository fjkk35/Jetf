using Service.EnumTax;
using Service.Models;
using Service.Services.SeaShenzhenOriginal;
using Service.Services.SeaShenzhenOriginal.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 新遞深圳稅金轉檔。
    /// </summary>
    public class SeaShenzhenFeeTransferController : Controller
    {
        private readonly SeaShenzhenFeeTransferService _seaShenzhenFeeTransferService;

        public SeaShenzhenFeeTransferController(SeaShenzhenFeeTransferService seaShenzhenFeeTransferService)
        {
            _seaShenzhenFeeTransferService = seaShenzhenFeeTransferService;
        }

        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult Transfer(SeaShenzhenFeeTransferRequest request)
        {
            try
            {
                var result = _seaShenzhenFeeTransferService.Transfer(request);
                const string message = "轉檔完成";

                return Json(new ResponseModel
                {
                    status = Status.success,
                    msg = message,
                    ReturnObject = new
                    {
                        result.DataDate,
                        result.SourceCount,
                        result.DeletedCount,
                        result.CreatedCount,
                        result.ExceptionCount,
                        Exceptions = result.Exceptions,
                        message
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel
                {
                    status = Status.error,
                    msg = ex.Message
                });
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaShenzhenOriginalUpload)]
        public JsonResult ExportExceptions(SeaShenzhenFeeTransferExceptionExportRequest request)
        {
            try
            {
                var fileBytes = _seaShenzhenFeeTransferService.ExportExceptionExcel(request);
                var fileGuid = Guid.NewGuid().ToString();
                var fileName = BuildExceptionFileName(request?.DataDate);

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

        private static string BuildExceptionFileName(string dataDate)
        {
            var label = string.IsNullOrWhiteSpace(dataDate)
                ? DateTime.Now.ToString("yyyyMMdd")
                : dataDate.Trim();

            return $"新遞稅金轉檔異常明細_{label}.xlsx";
        }
    }
}
