using Service.EnumTax;
using Service.Models;
using Service.Services.ReceivableCod;
using Service.Services.ReceivableCod.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 到付款應收未收明細功能。
    /// </summary>
    public sealed class ReceivableCodController : Controller
    {
        private readonly ReceivableCodService _service;

        /// <summary>
        /// 建立到付款應收未收明細控制器。
        /// </summary>
        /// <param name="service">到付款應收未收明細服務。</param>
        public ReceivableCodController(ReceivableCodService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示到付款應收未收明細查詢頁。
        /// </summary>
        /// <returns>查詢頁面。</returns>
        [UserAuthorize(Authority.Receivable)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 分頁查詢到付款應收未收明細。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>查詢結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.Receivable)]
        public JsonResult Search(ReceivableCodQueryRequest request)
        {
            try
            {
                return Json(new ResponseModel(_service.Search(request)));
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得客戶及客戶群組選項。
        /// </summary>
        /// <returns>客戶選擇彈窗資料。</returns>
        [HttpGet]
        [UserAuthorize(Authority.Receivable)]
        public JsonResult GetCustomerSelectionOptions()
        {
            try
            {
                return Json(
                    new ResponseModel(_service.GetCustomerSelectionOptions()),
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 匯出符合條件的全部到付款應收未收明細。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>Excel 檔案下載識別資訊。</returns>
        [HttpPost]
        [UserAuthorize(Authority.Receivable)]
        public JsonResult ExportExcel(ReceivableCodQueryRequest request)
        {
            try
            {
                var fileBytes = _service.ExportExcel(request);
                var fileName = $"到付款應收未收明細_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                var handle = Guid.NewGuid().ToString();
                TempData[handle] = fileBytes;

                return Json(new
                {
                    fileGuid = handle,
                    fileName,
                    msg = string.Empty
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    fileGuid = string.Empty,
                    fileName = string.Empty,
                    msg = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
