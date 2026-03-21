using JETFTAX.Models.SeaWorkErrorOrderReport;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Services.SeaWorkErrorOrderReport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaWorkErrorOrderReportController : Controller
    {
        private readonly SeaWorkErrorOrderReportService _seaWorkErrorOrderReportService;

        public SeaWorkErrorOrderReportController(SeaWorkErrorOrderReportService seaWorkErrorOrderReportService)
        {
            _seaWorkErrorOrderReportService = seaWorkErrorOrderReportService;
        }

        // GET: SeaWorkErrorOrderReport
        [UserAuthorize(Authority.SeaWorkErrorOrderReport)]
        public ActionResult Index()
        {
            return View();
        }

        [UserAuthorize(Authority.SeaWorkErrorOrderReport)]
        public ActionResult Download(string mainNumber)
        {
            string handle = Guid.NewGuid().ToString();
            var fileName = $"海快錯單作業.xlsx";
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
                    var workbook = _seaWorkErrorOrderReportService.GetSeaWorkErrorOrderWorkbook(mainNumberList);
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