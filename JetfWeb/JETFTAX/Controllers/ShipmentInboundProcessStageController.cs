using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.ShipmentInboundProcessStage;
using Service.Services.ShipmentInboundProcessStage.Domain;
using System;
using System.Linq;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 預先登記處理頁面的查詢與編輯控制器。
    /// </summary>
    public class ShipmentInboundProcessStageController : Controller
    {
        private readonly ShipmentInboundProcessStageService _service;

        public ShipmentInboundProcessStageController(ShipmentInboundProcessStageService service)
        {
            _service = service;
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult SearchData(ShipmentInboundProcessStageRequest request)
        {
            try
            {
                var result = _service.GetData(request);
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

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetProcessTypeList()
        {
            var list = Enum.GetValues(typeof(ShipmentInboundProcessType))
                .Cast<ShipmentInboundProcessType>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                })
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetProcessTransNoList()
        {
            var list = Enum.GetValues(typeof(ShipmentInboundProcessTransNo))
                .Cast<ShipmentInboundProcessTransNo>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                })
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetFreightPayerNoList()
        {
            var list = Enum.GetValues(typeof(ShipmentInboundFreightPayerNo))
                .Cast<ShipmentInboundFreightPayerNo>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                })
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult GetDetailById(int id)
        {
            try
            {
                return Json(_service.GetDetailById(id), JsonRequestBehavior.AllowGet);
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
        [UserAuthorize(Authority.ShipmentInboundProcess)]
        public JsonResult SaveProcess(ShipmentInboundProcessStageSaveRequest request)
        {
            try
            {
                var row = _service.SaveProcess(request);
                return Json(new ResponseModel(row));
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
