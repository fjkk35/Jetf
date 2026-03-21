using JETFTAX.Models.CainiaoFamilyTax;
using Service.EnumTax;
using Service.Extensions;
using Service.Services.CainiaoFamilyTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CainiaoFamilyTaxController : Controller
    {
        private readonly CainiaoFamilyTaxService _cainiaoFamilyTaxService;
        public CainiaoFamilyTaxController(CainiaoFamilyTaxService cainiaoFamilyTaxService) 
        {
            _cainiaoFamilyTaxService = cainiaoFamilyTaxService;
        }

        [UserAuthorize(Authority.CainiaoFamilyTax)]
        public ActionResult Index()
        {
            CainiaoFamilyTaxViewModel vm = new CainiaoFamilyTaxViewModel();
            List<SelectListItem> dateTimeList = new List<SelectListItem>();
            dateTimeList.Add(new SelectListItem() { Text = "前一天22:00-當日08:00", Value = "1" });
            dateTimeList.Add(new SelectListItem() { Text = "當日08:00-當日16:00", Value = "2" });
            dateTimeList.Add(new SelectListItem() { Text = "當日21:00-當日22:00", Value = "3" });
            vm.DateTimeList = dateTimeList;

            vm.StartDate = $"{DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")} 22:00:00";
            vm.EndDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 08:00:00";

            vm.CustomerList = EnumHelper.ToSelectList<EtlFamilyTax>();

            return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.CainiaoFamilyTax)]
        public ActionResult Download(CainiaoFamilyTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"{vm.Customer.ToDescription()}{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                var workbook = _cainiaoFamilyTaxService.GetCainiaoFamilyTax(vm.StartDate, vm.EndDate, vm.Customer);

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