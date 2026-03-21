using Service.Models;
using Service.Services.ScanCargoCustomerDiff;
using System;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class ScanCargoCustomerDiffController : Controller
    {
        private readonly ScanCargoCustomerDiffService _service;

        public ScanCargoCustomerDiffController(ScanCargoCustomerDiffService service)
        {
            _service = service;
        }

        /// <summary>
        /// 刷槍作業差異表
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得作業地區下拉選項
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetDataTypeList()
        {
            try
            {
                var list = _service.GetDataTypeList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 匯出 Excel
        /// </summary>
        /// <param name="startTime">開始時間</param>
        /// <param name="endTime">結束時間</param>
        /// <param name="dataType">作業地區</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ExportExcel(string startTime, string endTime, string dataType)
        {
            string handle = Guid.NewGuid().ToString();
            string fileName = $"{startTime}~{endTime}-刷槍作業差異表.xlsx";
            string msg = "";

            try
            {
                var workbook = _service.ExportExcel(startTime, endTime, dataType);

                using (var fileStream = new System.IO.MemoryStream())
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