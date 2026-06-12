using JETFTAX.Models;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.SeaCustomerShippingDetails;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaCustomerShippingDetailsController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly SeaCustomerShippingDetailsService _seaCustomerShippingDetailsService;

        public SeaCustomerShippingDetailsController(
            DropDownListService dropDownListService,
            SeaCustomerShippingDetailsService seaCustomerShippingDetailsService)
        {
            _dropDownListService = dropDownListService;
            _seaCustomerShippingDetailsService = seaCustomerShippingDetailsService;
        }

        [UserAuthorize(Authority.SeaCustomerShippingDetails)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [UserAuthorize(Authority.SeaCustomerShippingDetails)]
        public JsonResult GetWarehouseList()
        {
            return Json(_dropDownListService.GetSeaTaxTypeList(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [UserAuthorize(Authority.SeaCustomerShippingDetails)]
        public JsonResult GetCustomerList()
        {
            return Json(_dropDownListService.GetSeaCustomerList(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaCustomerShippingDetails)]
        public JsonResult DownloadExcel(SeaCustomerShippingDetailsRequest request)
        {
            try
            {
                var exportResult = _seaCustomerShippingDetailsService.Export(
                    request?.DataType,
                    request?.DespatchName,
                    request?.SDate,
                    request?.EDate);

                if (exportResult.status != Status.success ||
                    exportResult.FileBytes == null ||
                    exportResult.FileBytes.Length == 0)
                {
                    return Json(new
                    {
                        fileGuid = string.Empty,
                        fileName = string.Empty,
                        msg = exportResult.msg
                    });
                }

                var handle = Guid.NewGuid().ToString();
                TempData[handle] = exportResult.FileBytes;

                return Json(new
                {
                    fileGuid = handle,
                    fileName = exportResult.FileName,
                    msg = exportResult.msg
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    fileGuid = string.Empty,
                    fileName = string.Empty,
                    msg = ex.Message
                });
            }
        }
    }
}
