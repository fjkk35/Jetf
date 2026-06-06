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
    }
}
