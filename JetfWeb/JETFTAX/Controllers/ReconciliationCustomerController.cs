using Service.EnumTax;
using Service.Models;
using Service.Services.ReconciliationCustomer;
using Service.Services.ReconciliationCustomer.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 客戶銷帳功能。
    /// </summary>
    public sealed class ReconciliationCustomerController : Controller
    {
        private readonly ReconciliationCustomerService _service;

        /// <summary>
        /// 建立客戶銷帳控制器。
        /// </summary>
        /// <param name="service">客戶銷帳服務。</param>
        public ReconciliationCustomerController(ReconciliationCustomerService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示客戶銷帳頁面。
        /// </summary>
        /// <returns>客戶銷帳頁面。</returns>
        [UserAuthorize(Authority.ReconciliationCustomer)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢客戶應收金額。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>應收金額合計。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationCustomer)]
        public JsonResult Search(ReconciliationCustomerQueryRequest request)
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
        /// <returns>共用客戶選擇資料。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationCustomer)]
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
        /// 確認客戶銷帳。
        /// </summary>
        /// <param name="request">銷帳條件與輸入金額。</param>
        /// <returns>銷帳執行結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationCustomer)]
        public JsonResult Confirm(ReconciliationCustomerConfirmRequest request)
        {
            try
            {
                return Json(new ResponseModel(_service.Confirm(request)));
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}
