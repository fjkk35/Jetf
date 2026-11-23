using Service.EnumTax;
using Service.Services.SearchTax;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 稅金查詢控制器。
    /// </summary>
    public sealed class SearchTaxController : Controller
    {
        private readonly SearchTaxService _searchTaxService;

        /// <summary>
        /// 建立稅金查詢控制器。
        /// </summary>
        /// <param name="searchTaxService">稅金查詢服務。</param>
        public SearchTaxController(SearchTaxService searchTaxService)
        {
            _searchTaxService = searchTaxService;
        }

        /// <summary>
        /// 稅金查詢頁面。
        /// </summary>
        [UserAuthorize(Authority.SearchTax)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 依物流貨號、分提單號或袋號查詢費用主檔。
        /// </summary>
        /// <param name="keyword">查詢號碼。</param>
        [HttpPost]
        [UserAuthorize(Authority.SearchTax)]
        public JsonResult Search(string keyword)
        {
            try
            {
                return Json(new { Data = _searchTaxService.Search(keyword) });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 取得物流貨號的費用明細與配送進度。
        /// </summary>
        /// <param name="dlvInv">物流貨號。</param>
        [HttpPost]
        [UserAuthorize(Authority.SearchTax)]
        public JsonResult GetDetail(string dlvInv)
        {
            try
            {
                return Json(new { Data = _searchTaxService.GetDetail(dlvInv) });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 取得物流貨號的貨況查詢紀錄。
        /// </summary>
        /// <param name="dlvInv">物流貨號。</param>
        [HttpPost]
        [UserAuthorize(Authority.SearchTax)]
        public JsonResult GetSearchLogs(string dlvInv)
        {
            try
            {
                return Json(new { Data = _searchTaxService.GetSearchLogs(dlvInv) });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
