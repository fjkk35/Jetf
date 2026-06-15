using JETFTAX.Models;
using Service.EnumTax;
using Service.Models;
using Service.Services.SeaMainNumberShippingDetails;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaMainNumberShippingDetailsController : Controller
    {
        private readonly SeaMainNumberShippingDetailsService _seaMainNumberShippingDetailsService;

        public SeaMainNumberShippingDetailsController(SeaMainNumberShippingDetailsService seaMainNumberShippingDetailsService)
        {
            _seaMainNumberShippingDetailsService = seaMainNumberShippingDetailsService;
        }

        [UserAuthorize(Authority.SeaMainNumberShippingDetails)]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.SeaMainNumberShippingDetails)]
        public JsonResult DownloadExcel(SeaMainNumberShippingDetailsRequest request)
        {
            try
            {
                var exportResult = _seaMainNumberShippingDetailsService.Export(request?.MainNumbers);

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