using Service.Models;
using Service.Services.Ftz;
using Service.Services.Ftz.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class FtzController : Controller
    {
        private readonly FtzService _ftzService;

        public FtzController(FtzService ftzService)
        {
            _ftzService = ftzService;
        }

        // GET: Ftz
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 登入 Ftz 系統
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Login(FtzLoginRequest request)
        {
            try
            {
                var result = await _ftzService.LoginAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 查詢 Ftz 資料
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Query(FtzQueryRequest request)
        {
            try
            {
                var result = await _ftzService.QueryAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 主號查詢
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> QueryMain(FtzMainQueryRequest request, HttpPostedFileBase uploadFile)
        {
            try
            {
                var result = await _ftzService.MainQueryAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 併袋號查詢
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> QueryBag(FtzBagQueryRequest request)
        {
            try
            {
                var result = await _ftzService.QueryBagAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> ExportExcel(FtzQueryRequest request)
        {
            try
            {
                var workbook = await _ftzService.ExportExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Ftz查詢結果_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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

        /// <summary>
        /// 主號查詢匯出 Excel
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> ExportMainExcel(FtzMainQueryRequest request, HttpPostedFileBase uploadFile)
        {
            try
            {
                List<FtzMainUploadExcelRow> uploadRows = null;

                if (uploadFile != null && uploadFile.ContentLength > 0)
                {
                    uploadRows = _ftzService.ReadMainUploadRows(uploadFile.InputStream);
                }

                var workbook = await _ftzService.ExportMainExcel(request, uploadRows);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Ftz主號查詢結果_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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

        /// <summary>
        /// 併袋號查詢匯出 Excel
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> ExportBagExcel(FtzBagQueryRequest request)
        {
            try
            {
                var workbook = await _ftzService.ExportBagExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Ftz併袋號查詢結果_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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
