using Service.EnumTax;
using Service.Services.ShipmentInboundExceptionRecord;
using Service.Services.ShipmentInboundExceptionRecord.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 貨件回倉異常紀錄查詢 Controller。
    /// </summary>
    public class ShipmentInboundExceptionRecordController : Controller
    {
        private readonly ShipmentInboundExceptionRecordService _service;

        /// <summary>
        /// 初始化貨件回倉異常紀錄查詢 Controller。
        /// </summary>
        /// <param name="service">貨件回倉異常紀錄服務。</param>
        public ShipmentInboundExceptionRecordController(ShipmentInboundExceptionRecordService service)
        {
            _service = service;
        }

        /// <summary>
        /// 異常紀錄查詢頁面。
        /// </summary>
        /// <returns>異常紀錄查詢頁面。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢異常紀錄資料。
        /// </summary>
        /// <param name="searchRequest">查詢條件。</param>
        /// <returns>異常紀錄查詢結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult SearchData(ShipmentInboundExceptionRecordRequest searchRequest)
        {
            try
            {
                var result = _service.GetData(searchRequest);
                return Json(new
                {
                    Data = result.Data,
                    TotalCount = result.TotalCount
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得異常原因下拉清單。
        /// </summary>
        /// <returns>異常原因下拉清單。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult GetExceptionReasonList()
        {
            try
            {
                var list = _service.GetExceptionReasonList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 匯出異常件紀錄 Excel 與圖片 ZIP。
        /// </summary>
        /// <param name="searchRequest">查詢條件。</param>
        /// <returns>暫存下載檔案資訊。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ShipmentInboundRecord)]
        public JsonResult ExportExcel(ShipmentInboundExceptionRecordRequest searchRequest)
        {
            try
            {
                var result = _service.ExportExcelZip(searchRequest, Server.MapPath);
                var handle = Guid.NewGuid().ToString();
                TempData[handle] = result.FileBytes;

                return Json(new { fileGuid = handle, fileName = result.FileName, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { fileGuid = "", fileName = "", msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
