using JETFTAX.Models.CptStatusReport;
using JETFTAX.Models.CptTradeVan;
using JETFTAX.Models.SeaUnreceivedOrder;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Extensions;
using Service.Models;
using Service.Services.CptStatusReport;
using Service.Services.SeaUnreceivedOrder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaUnreceivedOrderController : Controller
    {
        private readonly SeaUnreceivedOrderService _seaUnreceivedOrderService;
        public SeaUnreceivedOrderController(SeaUnreceivedOrderService seaUnreceivedOrderService)
        {
            _seaUnreceivedOrderService = seaUnreceivedOrderService;
        }

        [UserAuthorize(Authority.SeaUnreceivedOrder)]
        public ActionResult Index()
        {
          SeaUnreceivedOrderViewModel vm = new SeaUnreceivedOrderViewModel()
          {
              DataTypeList = EnumHelper.ToSelectList<SeaErrorReportEnum>(),
          };
          return View(vm);
        }

        [UserAuthorize(Authority.SeaUnreceivedOrder)]
        public ActionResult GetExcel(SeaUnreceivedOrderViewModel vm)
        {
            string fileName = $"海快{vm.DataType.ToDescription()}.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            var result = _seaUnreceivedOrderService.GetExecl(vm.MainNumber,vm.DataType);

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