using JETFTAX.Models.CainiaoTaixinStarTax;
using Service.EnumTax;
using Service.Services.CainiaoTaixinStarTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CainiaoTaixinStarTaxController : Controller
    {
        private readonly CainiaoTaixinStarTaxService _cainiaoTaixinStarTaxService;

        public CainiaoTaixinStarTaxController(CainiaoTaixinStarTaxService cainiaoTaixinStarTaxService)
        {
            _cainiaoTaixinStarTaxService = cainiaoTaixinStarTaxService;
        }

        public ActionResult Index()
        {
            CainiaoTaixinStarTaxViewModel vm = new CainiaoTaixinStarTaxViewModel();
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
        [UserAuthorize(Authority.CainiaoTaixinStarTax)]
        public ActionResult Download(CainiaoTaixinStarTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"菜鳥超峰稅金{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                var workbook = _cainiaoTaixinStarTaxService.GetWorkbook(vm.StartDate, vm.EndDate);

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