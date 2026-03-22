using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Services.SjlBilling;
using Service.Services.SjlBilling.Domain;
using System;
using System.IO;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SjlBillingController : Controller
    {
        private readonly SjlBillingService _sjlBillingService;

        public SjlBillingController(SjlBillingService sjlBillingService)
        {
            _sjlBillingService = sjlBillingService;
        }

        /// <summary>
        /// 捷利帳單頁面。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.SjlBilling)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 下載捷利帳單 Excel。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>下載資訊。</returns>
        [HttpPost]
        [UserAuthorize(Authority.SjlBilling)]
        public JsonResult DownloadExcel(SjlBillingQueryRequest request)
        {
            string handle = Guid.NewGuid().ToString();
            string fileName = string.Format("{0}_{1}_{2}_捷利帳單.xlsx", request.StartDate, request.EndDate, request.TransName);
            string msg = string.Empty;

            try
            {
                IWorkbook workbook = _sjlBillingService.GetWorkbook(request);

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

            return Json(new { fileGuid = handle, fileName = fileName, msg = msg });
        }
    }
}
