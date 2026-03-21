using JETFTAX.Models.CainiaoHiLifeTax;
using JETFTAX.Models.CainiaoHiLifeTaxDetails;
using JETFTAX.Models.HctEtlTax;
using Service.EnumTax;
using Service.Services;
using Service.Services.CainiaoHiLifeTaxDetails;
using Service.Services.HctEtlTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class HctEtlTaxController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly HctEtlTaxService _hctEtlTaxService;

        public HctEtlTaxController(HctEtlTaxService hctEtlTaxService, DropDownListService dropDownListService)
        {
            _hctEtlTaxService = hctEtlTaxService;
            _dropDownListService = dropDownListService;
        }

        [UserAuthorize(Authority.HctEtlTax)]
        public ActionResult Index()
        {
            var vm = new HctEtlTaxViewModel();
            List<SelectListItem> dateTimeList = new List<SelectListItem>();
            dateTimeList.Add(new SelectListItem() { Text = "前一天22:00-當日08:00", Value = "1" });
            dateTimeList.Add(new SelectListItem() { Text = "當日08:00-當日16:00", Value = "2" });
            dateTimeList.Add(new SelectListItem() { Text = "當日21:00-當日22:00", Value = "3" });
            vm.DateTimeList = dateTimeList;

            vm.StartDate = $"{DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")} 22:00:00";
            vm.EndDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 08:00:00";

            vm.CustomerList = _dropDownListService.GetEtlCustomerList();


            return View(vm);
        }

        [UserAuthorize(Authority.HctEtlTax)]
        public ActionResult GetExcel(HctEtlTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"空快客戶託運新竹明細表{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                var workbook = _hctEtlTaxService.GetWorkbook(vm.CustCode,vm.StartDate, vm.EndDate);

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