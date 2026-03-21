using Service.EnumTax;
using Service.Models;
using Service.Services.BatchSearchTax;
using Service.Services.BatchSearchTax.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class BatchSearchTaxController : Controller
    {
        private readonly BatchSearchTaxService _batchSearchTaxService;

        public BatchSearchTaxController(BatchSearchTaxService batchSearchTaxService)
        {
            _batchSearchTaxService = batchSearchTaxService;
        }

        // GET: BatchSearchTax
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.BatchSearchTax)]
        public JsonResult ExportExcel(BatchSearchTaxRequest request)
        {
            try
            {
                var workbook = _batchSearchTaxService.ExportExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"批量稅金查詢_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }

                return new JsonResult()
                {
                    Data = new { fileGuid = handle, fileName = fileName, msg = "" }
                };
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel($"匯出失敗：{ex.Message}"));
            }
        }
    }
}