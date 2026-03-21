using JETFTAX.Models.CainiaoFamilyTax;
using JETFTAX.Models.CainiaoTaixinStarTax;
using JETFTAX.Models.SevenElevenEtlTax;
using JETFTAX.Models.TCatEtlTax;
using Service.EnumTax;
using Service.Services;
using Service.Services.CainiaoFamilyTax;
using Service.Services.SevenElevenEtlTaxTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class TCatEtlTaxController : Controller
    {
        private readonly TCatEtlTaxService _tcatEtlTaxService;

        public TCatEtlTaxController(TCatEtlTaxService tcatEtlTaxService)
        {
            _tcatEtlTaxService = tcatEtlTaxService;
        }

        // GET: TCatEtlTax
        public ActionResult Index()
        {
            TCatEtlTaxViewModel vm = new TCatEtlTaxViewModel();
            List<SelectListItem> dateTimeList = new List<SelectListItem>();
            dateTimeList.Add(new SelectListItem() { Text = "前一天22:00-當日08:00", Value = "1" });
            dateTimeList.Add(new SelectListItem() { Text = "當日08:00-當日16:00", Value = "2" });
            dateTimeList.Add(new SelectListItem() { Text = "當日21:00-當日22:00", Value = "3" });
            vm.DateTimeList = dateTimeList;

            vm.StartDate = $"{DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")} 22:00:00";
            vm.EndDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 08:00:00";

            return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.CainiaoFamilyTax)]
        public ActionResult Download(TCatEtlTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"黑貓{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                var workbook = _tcatEtlTaxService.GetTCatEtlTax(vm.StartDate, vm.EndDate);

                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };
        }
    }
}