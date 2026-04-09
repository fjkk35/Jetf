using Service.EnumTax;
using Service.Models;
using Service.Services.BatchSearchShenzhenCargo;
using Service.Services.BatchSearchShenzhenCargo.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class BatchSearchShenzhenCargoController : Controller
    {
        private readonly BatchSearchShenzhenCargoService _batchSearchShenzhenCargoService;

        public BatchSearchShenzhenCargoController(BatchSearchShenzhenCargoService batchSearchShenzhenCargoService)
        {
            _batchSearchShenzhenCargoService = batchSearchShenzhenCargoService;
        }

        // GET: BatchSearchShenzhenCargo
        [UserAuthorize(Authority.BatchSearchShenzhenCargo)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢速派新遞物流貨號
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.BatchSearchShenzhenCargo)]
        public JsonResult Query(BatchSearchShenzhenCargoRequest request)
        {
            try
            {
                var result = _batchSearchShenzhenCargoService.QueryShenzhenCargo(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel($"查詢失敗：{ex.Message}"));
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.BatchSearchShenzhenCargo)]
        public JsonResult ExportExcel(BatchSearchShenzhenCargoRequest request)
        {
            try
            {
                var workbook = _batchSearchShenzhenCargoService.ExportExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"批量查詢速派新遞物流貨號_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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
                return Json(new ResponseModel($"匯出失敗：{ex.Message}"));
            }
        }
    }
}