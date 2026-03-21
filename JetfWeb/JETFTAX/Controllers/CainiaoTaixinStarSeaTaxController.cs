using JETFTAX.Models.CainiaoSevenElevenSeaTax;
using Service.EnumTax;
using Service.Services.CainiaoSevenElevenSeaTax;
using Service.Services.CainiaoTaixinStarSeaTax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CainiaoTaixinStarSeaTaxController : Controller
    {
        private readonly CainiaoTaixinStarSeaTaxService _cainiaoTaixinStarSeaTaxService;

        public CainiaoTaixinStarSeaTaxController(CainiaoTaixinStarSeaTaxService cainiaoTaixinStarSeaTaxService)
        {
            _cainiaoTaixinStarSeaTaxService = cainiaoTaixinStarSeaTaxService;
        }

        // GET: CainiaoTaixinStarSeaTax
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [UserAuthorize(Authority.CainiaoTaixinStarSeaTax)]
        public ActionResult Download(DateTime dataDate)
        {
            var handle = Guid.NewGuid().ToString();
            var fileName = $"菜鳥海運超峰稅金{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
            var msg = "";
            try
            {
                var workbook = _cainiaoTaixinStarSeaTaxService.GetWorkbook(dataDate);
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