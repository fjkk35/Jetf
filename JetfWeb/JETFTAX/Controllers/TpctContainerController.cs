using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class TpctContainerController : Controller
    {
        TpctContainerService tpctContainerService = new TpctContainerService();

        // GET: TpctContainer
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Download(string data)
        {
            string handle = Guid.NewGuid().ToString();
            string fileName = $"{DateTime.Now.Date.ToString("yyyyMMdd")}TPCT貨櫃動態查詢.xlsx";
            string msg = "";
            try
            {
                List<string> list = data.Split('\n')
                                       .Select(line => (line.Length > 11 ? line.Substring(0, 11) : line))
                                       .Where(line => !string.IsNullOrWhiteSpace(line))
                                       .ToList();

                IWorkbook workbook = tpctContainerService.Download(list);

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