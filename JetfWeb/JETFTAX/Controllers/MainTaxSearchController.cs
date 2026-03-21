using Service.EnumTax;
using Service.Models;
using Service.Services.MainTaxSearch;
using Service.Services.MainTaxSearch.Domain;
using System;
using System.IO;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class MainTaxSearchController : Controller
    {
        private readonly MainTaxSearchService _mainTaxSearchService;

        public MainTaxSearchController(MainTaxSearchService mainTaxSearchService)
        {
            _mainTaxSearchService = mainTaxSearchService;
        }

        // GET: MainTaxSearch
        [UserAuthorize(Authority.MainTaxSearch)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.MainTaxSearch)]
        public JsonResult ExportExcel(MainTaxSearchRequest request)
        {
            try
            {
                var workbook = _mainTaxSearchService.ExportExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"主號稅金查詢_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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