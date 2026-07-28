using Service.Models;
using Service.Services.ReconciliationIncludeTaxFormat;
using Service.Services.ReconciliationIncludeTaxFormat.Domain;
using Service.EnumTax;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 包稅客戶 Excel 匯出格式管理控制器。
    /// </summary>
    public sealed class ReconciliationIncludeTaxFormatController : Controller
    {
        private readonly ReconciliationIncludeTaxFormatService _service;

        /// <summary>
        /// 建立包稅客戶格式控制器。
        /// </summary>
        /// <param name="service">包稅客戶格式服務。</param>
        public ReconciliationIncludeTaxFormatController(
            ReconciliationIncludeTaxFormatService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示包稅客戶格式管理頁面。
        /// </summary>
        /// <returns>格式管理頁面。</returns>
        [UserAuthorize(Authority.ReconciliationIncludeTaxFormat)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢所有包稅客戶匯出格式。
        /// </summary>
        /// <returns>格式清單。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationIncludeTaxFormat)]
        public JsonResult Search()
        {
            try
            {
                return Json(
                    new ResponseModel(_service.Search()),
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得可對應的 FEE_MASTER 與 FEE_MASTER_DETAIL 欄位。
        /// </summary>
        /// <returns>欄位選項。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationIncludeTaxFormat)]
        public JsonResult GetFieldOptions()
        {
            try
            {
                return Json(
                    new ResponseModel(_service.GetFieldOptions()),
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得單一格式設定。
        /// </summary>
        /// <param name="id">格式識別碼。</param>
        /// <returns>格式明細。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationIncludeTaxFormat)]
        public JsonResult GetDetail(int id)
        {
            try
            {
                return Json(
                    new ResponseModel(_service.GetDetail(id)),
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 儲存包稅客戶匯出格式。
        /// </summary>
        /// <param name="request">格式儲存請求。</param>
        /// <returns>儲存結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationIncludeTaxFormat)]
        public JsonResult Save(ReconciliationIncludeTaxFormatSaveRequest request)
        {
            try
            {
                _service.Save(request);
                return Json(new ResponseModel());
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除包稅客戶匯出格式。
        /// </summary>
        /// <param name="id">格式識別碼。</param>
        /// <returns>刪除結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationIncludeTaxFormat)]
        public JsonResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return Json(new ResponseModel());
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}
