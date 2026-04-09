using Service.Models;
using Service.Services.Tact;
using Service.Services.Tact.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class TactController : Controller
    {
        private readonly TactService _tactService;

        public TactController(TactService tactService)
        {
            _tactService = tactService;
        }

        // GET: Tact
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 登入 Tact 系統
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Login(TactLoginRequest request)
        {
            try
            {
                var result = await _tactService.LoginAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 查詢 Tact 資料
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> Query(TactQueryRequest request)
        {
            try
            {
                var result = await _tactService.QueryAsync(request);
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
        public async Task<JsonResult> QueryMain(TactMainQueryRequest request)
        {
            try
            {
                var result = await _tactService.MainQueryAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 匯出 Excel（分號查詢）
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> ExportExcel(TactQueryRequest request)
        {
            try
            {
                var workbook = await _tactService.ExportExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Tact查詢結果_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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
        public async Task<JsonResult> ExportMainExcel(TactMainQueryRequest request)
        {
            try
            {
                var workbook = await _tactService.ExportMainExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Tact主號查詢結果_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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
        /// 併袋號查詢
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> QueryBag(TactBagQueryRequest request)
        {
            try
            {
                var result = await _tactService.QueryBagAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 併袋號查詢匯出 Excel
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> ExportBagExcel(TactBagQueryRequest request)
        {
            try
            {
                var workbook = await _tactService.ExportBagExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Tact併袋號查詢結果_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

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