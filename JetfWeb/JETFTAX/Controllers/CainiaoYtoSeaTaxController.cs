using JETFTAX.Models.CainiaoYtoSeaTax;
using Service.EnumTax;
using Service.Services.CainiaoYtoSeaTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CainiaoYtoSeaTaxController : Controller
    {
        private readonly CainiaoYtoSeaTaxService _cainiaoYtoSeaTaxService;

        public CainiaoYtoSeaTaxController(CainiaoYtoSeaTaxService cainiaoYtoSeaTaxService) 
        {
            _cainiaoYtoSeaTaxService = cainiaoYtoSeaTaxService;
        }

        [UserAuthorize(Authority.CainiaoYtoSeaTax)]
        public ActionResult Index()
        {
            CainiaoYtoSeaTaxViewModel vm = new CainiaoYtoSeaTaxViewModel();
            vm.DataDate = DateTime.Now.ToString("yyyy-MM-dd");
            return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.CainiaoYtoSeaTax)]
        public ActionResult Download(CainiaoYtoSeaTaxViewModel vm)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"菜鳥圓通海運稅金{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                string dataDate = Convert.ToDateTime(vm.DataDate).ToString("yyyyMMdd");
                var workbook = _cainiaoYtoSeaTaxService.GetCainiaoYtoSeaTax(dataDate);
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