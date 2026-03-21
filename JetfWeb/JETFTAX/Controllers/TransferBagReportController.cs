using JETFTAX.Models.CCLWork;
using JETFTAX.Models.TransferBagReport;
using NPOI.SS.UserModel;
using Service.Services;
using Service.Services.ScanCargoCustomer;
using Service.Services.TransferBagReport;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class TransferBagReportController : Controller
    {
        private readonly TransferBagReportService _transferBagReportService;

        public TransferBagReportController(TransferBagReportService transferBagReportService)
        {
            _transferBagReportService = transferBagReportService;
        }

        // GET: TransferBagReport
        public ActionResult Index()
        {
            var vm = new TransferBagReportViewModel();
            DateTime date = DateTime.Now;
            vm.StartDate = $"{date.ToString("yyyy-MM-dd")} 00:00:00";
            vm.EndDate = $"{date.ToString("yyyy-MM-dd")} 23:59:59";
            return View(vm);
        }

        public ActionResult Excel(TransferBagReportViewModel vm)
        {
            string fileName = $"接駁袋數統計表.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";

            try
            {
                var result = _transferBagReportService.GetWorkbook(vm.StartDate, vm.EndDate);

                using (MemoryStream fileStream = new MemoryStream())
                {
                    var workbook = result;
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