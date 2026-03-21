using NPOI.SS.UserModel;
using Service.Models;
using Service.Services.BatchUploadProcess;
using Service.Services.BusinessRegistryNew;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class BusinessRegistryNewController : Controller
    {
        private readonly BusinessRegistryNewService _businessRegistryNewService;

        public BusinessRegistryNewController(BusinessRegistryNewService businessRegistryNewService)
        {
            _businessRegistryNewService = businessRegistryNewService;
        }

        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Search(string businessId)
        {
            var result = await _businessRegistryNewService.Search(businessId);

            return Json(result);
        }

        public async Task<ActionResult> GetExcel(string businessId)
        {
            string fileName = $"營業登記查詢結果.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            var result = await _businessRegistryNewService.GetExecl(businessId);

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