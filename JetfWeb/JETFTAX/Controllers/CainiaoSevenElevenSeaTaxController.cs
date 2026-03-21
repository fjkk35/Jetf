using JETFTAX.Models.CainiaoSevenElevenSeaTax;
using Service.EnumTax;
using Service.Services.CainiaoSevenElevenSeaTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CainiaoSevenElevenSeaTaxController : Controller
    {
        private readonly CainiaoSevenElevenSeaTaxService _cainiaoSevenElevenSeaTaxService;

        public CainiaoSevenElevenSeaTaxController(CainiaoSevenElevenSeaTaxService cainiaoSevenElevenSeaTaxService) 
        {
            _cainiaoSevenElevenSeaTaxService = cainiaoSevenElevenSeaTaxService;
        }

        [UserAuthorize(Authority.CainiaoSevenElevenSeaTax)]
        public ActionResult Index()
        {
            CainiaoSevenElevenSeaTaxViewModel vm = new CainiaoSevenElevenSeaTaxViewModel();
            vm.DataDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.CainiaoSevenElevenSeaTax)]
        public ActionResult Download(CainiaoSevenElevenSeaTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"菜鳥7-11海運稅金{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                string dataDate = Convert.ToDateTime(vm.DataDate).ToString("yyyyMMdd");
                var workbook = _cainiaoSevenElevenSeaTaxService.GetCainiaoSevenElevenSeaTax(dataDate);
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