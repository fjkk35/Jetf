using Service.EnumTax;
using Service.Services.SeaWorkErrorOrderReport;
using Service.Services.SeaWorkRecognizance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaWorkRecognizanceController : Controller
    {
        private readonly SeaWorkRecognizanceService _seaWorkRecognizanceService;

        public SeaWorkRecognizanceController(SeaWorkRecognizanceService seaWorkRecognizanceService)
        {
            _seaWorkRecognizanceService = seaWorkRecognizanceService;
        }

        // GET: SeaWorkRecognizance
        [UserAuthorize(Authority.SeaWorkRecognizance)]
        public ActionResult Index()
        {
            return View();
        }

        [UserAuthorize(Authority.SeaWorkRecognizance)]
        public ActionResult Download(string mainNumber)
        {
            string handle = Guid.NewGuid().ToString();
            var fileName = $"海快作業具結.xlsx";
            string msg = "";

            try
            {
                var mainNumberList = mainNumber
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r.Trim())
                    .Distinct()
                    .ToList();

                using (MemoryStream fileStream = new MemoryStream())
                {
                    var workbook = _seaWorkRecognizanceService.GetExcel(mainNumberList);
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