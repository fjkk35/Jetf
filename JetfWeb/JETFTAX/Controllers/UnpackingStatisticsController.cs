using Service.EnumTax;
using Service.Models;
using Service.Services.UnpackingStatistics;
using System;
using System.IO;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class UnpackingStatisticsController : Controller
    {
        private readonly UnpackingStatisticsService _service;

        public UnpackingStatisticsController(UnpackingStatisticsService service) 
        {
            _service = service;
        }

        [UserAuthorize(Authority.UnpackingStatistics)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得拆袋統計彙總資料(JSON) 不使用 ViewModel
        /// </summary>
        [HttpGet]
        [UserAuthorize(Authority.UnpackingStatistics)]
        public ActionResult GetData(string startDate, string endDate)
        {
            try
            {
                var data = _service.GetPivotData(startDate, endDate);
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.UnpackingStatistics)]
        public ActionResult Export(string startDate, string endDate)
        {
            var fileName = $"拆袋統計表_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            try
            {
                // 檢查參數是否為空
                if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                {
                    Response.StatusCode = 400;
                    Response.ContentType = "application/json";
                    return Json(new { error = true, msg = "日期參數不能為空" });
                }

                var wb = _service.GetWorkbook(startDate, endDate);
                using (var ms = new MemoryStream())
                {
                    wb.Write(ms);
                    var data = ms.ToArray();
                    
                    // 檢查資料是否有效
                    if (data == null || data.Length == 0)
                    {
                        Response.StatusCode = 500;
                        Response.ContentType = "application/json";
                        return Json(new { error = true, msg = "產生檔案失敗" });
                    }
                    
                    // 直接返回檔案流給前端 Blob API 使用
                    return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                // 錯誤時返回 JSON，前端可以透過 Content-Type 判斷
                Response.StatusCode = 500;
                Response.ContentType = "application/json";
                return Json(new { error = true, msg = ex.Message });
            }
        }
    }
}