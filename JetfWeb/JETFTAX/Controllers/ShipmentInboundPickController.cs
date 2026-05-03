using Service.EnumTax;
using Service.Extensions;
using Service.Services.ShipmentInboundPick;
using Service.Services.ShipmentInboundPick.Domain;
using System;
using System.Linq;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ShipmentInboundPickController : Controller
    {
        private readonly ShipmentInboundPickService _shipmentInboundPickService;

        public ShipmentInboundPickController(ShipmentInboundPickService shipmentInboundPickService)
        {
            _shipmentInboundPickService = shipmentInboundPickService;
        }

        [UserAuthorize(Authority.ShipmentInboundPick)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundPick)]
        public JsonResult GetProcessTypeList()
        {
            var list = Enum.GetValues(typeof(ShipmentInboundProcessType))
                .Cast<ShipmentInboundProcessType>()
                .Select(item => new
                {
                    Value = (byte)item,
                    Text = item.ToDescription()
                }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundPick)]
        public JsonResult SearchData(ShipmentInboundPickRequest searchRequest)
        {
            try
            {
                var result = _shipmentInboundPickService.GetData(searchRequest);

                return Json(new
                {
                    Data = result
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
        [UserAuthorize(Authority.ShipmentInboundPick)]
        public ActionResult ExportExcel(ShipmentInboundPickRequest searchRequest)
        {
            try
            {
                var data = _shipmentInboundPickService.GetData(searchRequest);
                var excelData = _shipmentInboundPickService.ExportToExcel(data);

                var fileName = $"撿貨明細_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
