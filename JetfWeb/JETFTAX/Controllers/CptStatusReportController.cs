using JETFTAX.Models.CptStatusReport;
using JETFTAX.Models.CptTradeVan;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.CptStatusReport;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class CptStatusReportController : Controller
    {
        private readonly CptStatusReportService _cptStatusReportService;

        public CptStatusReportController(CptStatusReportService cptStatusReportService)
        {
            _cptStatusReportService = cptStatusReportService;
        }

        // GET: CptStatusReport
        public ActionResult Index()
        {
            var vm = new CptStatusReportViewModel()
            {
                DataTypeList = EnumHelper.ToSelectList<DataTypeEnum>(),
                CptStatusList = EnumHelper.ToSelectList<CptStatusEnum>(),
                StartDate = DateTime.Now.ToString("yyyy-MM-dd"),
                EndDate = DateTime.Now.ToString("yyyy-MM-dd"),
            };

            return View(vm);
        }

        public ActionResult GetExcel(CptStatusReportViewModel vm)
        {
            string fileName = $"{vm.StartDate}～{vm.EndDate}-{vm.DataType.ToDescription()}主號查詢明細.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            var result = _cptStatusReportService.GetExecl(vm.DataType,vm.CptStatus, vm.StartDate, vm.EndDate);

            if (result.status == Status.error)
            {
                return new JsonResult()
                {
                    Data = new { msg = result.msg }
                };
            }

            using (MemoryStream fileStream = new MemoryStream())
            {
                var workbook = result.ReturnObject as IWorkbook;
                workbook.Write(fileStream);
                TempData[handle] = fileStream.ToArray();
            }

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };

        }
    }
}