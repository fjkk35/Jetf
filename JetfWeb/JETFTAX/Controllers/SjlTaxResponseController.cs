using Service.EnumTax;
using Service.Models;
using Service.Services.SjlTaxResponse;
using Service.Services.SjlTaxResponse.Domain;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 捷利稅金回傳控制器。
    /// </summary>
    public class SjlTaxResponseController : Controller
    {
        private readonly SjlTaxResponseService _sjlTaxResponseService;

        /// <summary>
        /// 建立捷利稅金回傳控制器。
        /// </summary>
        /// <param name="sjlTaxResponseService">捷利稅金手動回傳服務。</param>
        public SjlTaxResponseController(SjlTaxResponseService sjlTaxResponseService)
        {
            _sjlTaxResponseService = sjlTaxResponseService;
        }

        /// <summary>
        /// 捷利稅金回傳頁面。
        /// </summary>
        /// <returns>頁面。</returns>
        [UserAuthorize(Authority.SjlTaxResponse)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 依物流貨號手動回傳捷利稅金。
        /// </summary>
        /// <param name="request">稅金類型與物流貨號。</param>
        /// <returns>回傳結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.SjlTaxResponse)]
        public async Task<JsonResult> SendSjlTax(SjlTaxManualRequestModel request)
        {
            try
            {
                var result = await _sjlTaxResponseService.SendManualTaxAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}
