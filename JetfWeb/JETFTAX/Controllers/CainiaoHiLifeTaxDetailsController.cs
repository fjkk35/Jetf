using JETFTAX.Models.CainiaoHiLifeTaxDetails;
using Service.EnumTax;
using Service.Services.CainiaoHiLifeTaxDetails;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CainiaoHiLifeTaxDetailsController : Controller
    {
        private readonly CainiaoHiLifeTaxDetailsService _cainiaoHiLifeTaxDetailsService;
        public CainiaoHiLifeTaxDetailsController(CainiaoHiLifeTaxDetailsService cainiaoHiLifeTaxDetailsService) 
        {
            _cainiaoHiLifeTaxDetailsService = cainiaoHiLifeTaxDetailsService;
        }

        

        [UserAuthorize(Authority.CainiaoHiLifeTax)]
        public ActionResult Index()
        {
            CainiaoHiLifeTaxDetailsViewModel vm = new CainiaoHiLifeTaxDetailsViewModel();
            List<SelectListItem> dateTimeList = new List<SelectListItem>();
            vm.StartDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 00:00:00";
            vm.EndDate = $"{DateTime.Now.ToString("yyyy-MM-dd")} 23:59:59";

            return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.CainiaoHiLifeTax)]
        public ActionResult GetExcel(CainiaoHiLifeTaxDetailsViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"萊爾富接收稅金明細表{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                var workbook = _cainiaoHiLifeTaxDetailsService.GetWorkbook(vm.StartDate, vm.EndDate);

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