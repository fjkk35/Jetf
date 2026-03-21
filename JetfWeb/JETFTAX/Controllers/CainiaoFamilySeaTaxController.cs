using JETFTAX.Models.CainiaoFamilySeaTax;
using JETFTAX.Models.CainiaoSevenElevenSeaTax;
using Service.EnumTax;
using Service.Services.CainiaoFamilySeaTax;
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
    public class CainiaoFamilySeaTaxController : Controller
    {
        private readonly CainiaoFamilySeaTaxService _cainiaoFamilySeaTaxService;

        public CainiaoFamilySeaTaxController(CainiaoFamilySeaTaxService cainiaoFamilySeaTaxService)
        {
            _cainiaoFamilySeaTaxService = cainiaoFamilySeaTaxService;
        }

        // GET: CainiaoFamilySeaTax
        public ActionResult Index()
        {
            var vm = new CainiaoFamilySeaTaxViewModel();
            vm.DataDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        [HttpPost]
        public ActionResult Download(CainiaoFamilySeaTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"菜鳥全家海運稅金{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                string dataDate = Convert.ToDateTime(vm.DataDate).ToString("yyyyMMdd");
                var workbook = _cainiaoFamilySeaTaxService.GetCainiaoFamilySeaTax(dataDate);
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