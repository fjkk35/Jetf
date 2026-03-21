using JETFTAX.Models.SeaUnreceivedOrder;
using NPOI.SS.UserModel;
using Org.BouncyCastle.X509;
using Service.Models;
using Service.Services.BatchUploadProcess;
using Service.Services.SeaUnreceivedOrder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class BusinessRegistryController : Controller
    {
        private readonly BusinessRegistryService _businessRegistryService;

        public BusinessRegistryController(BusinessRegistryService businessRegistryService) 
        {
            _businessRegistryService = businessRegistryService;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetExcel(string businessId)
        {
            string fileName = $"營業登記查詢結果.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            var result = _businessRegistryService.GetExecl(businessId);

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