using Service.EnumTax;
using Service.Models;
using Service.Services.ReconciliationCustomerGroup;
using Service.Services.ReconciliationCustomerGroup.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    /// <summary>
    /// 代收銷帳客戶群組控制器。
    /// </summary>
    public sealed class ReconciliationCustomerGroupController : Controller
    {
        private readonly ReconciliationCustomerGroupService _service;

        /// <summary>
        /// 建立代收銷帳客戶群組控制器。
        /// </summary>
        /// <param name="service">代收銷帳客戶群組服務。</param>
        public ReconciliationCustomerGroupController(ReconciliationCustomerGroupService service)
        {
            _service = service;
        }

        /// <summary>
        /// 顯示代收銷帳客戶群組頁面。
        /// </summary>
        /// <returns>客戶群組頁面。</returns>
        [UserAuthorize(Authority.ReconciliationCustomerGroup)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢代收銷帳客戶群組。
        /// </summary>
        /// <param name="request">查詢條件。</param>
        /// <returns>查詢結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationCustomerGroup)]
        public JsonResult Search(ReconciliationCustomerGroupQueryRequest request)
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
        /// 取得客戶群組下拉選項。
        /// </summary>
        /// <param name="type">運送類型代碼。</param>
        /// <returns>客戶群組選項。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationCustomerGroup)]
        public JsonResult GetGroupOptions(string type)
        {
            try
            {
                return Json(new ResponseModel(_service.GetGroupOptions(type)), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得客戶勾選選項。
        /// </summary>
        /// <param name="type">運送類型代碼。</param>
        /// <param name="id">目前編輯的客戶群組識別碼。</param>
        /// <returns>客戶勾選選項。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationCustomerGroup)]
        public JsonResult GetCustomerOptions(string type, int? id)
        {
            try
            {
                return Json(new ResponseModel(_service.GetCustomerOptions(type, id)), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得客戶群組編輯資料。
        /// </summary>
        /// <param name="id">客戶群組識別碼。</param>
        /// <returns>客戶群組編輯資料。</returns>
        [HttpGet]
        [UserAuthorize(Authority.ReconciliationCustomerGroup)]
        public JsonResult GetDetail(int id)
        {
            try
            {
                return Json(new ResponseModel(_service.GetDetail(id)), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增或修改客戶群組。
        /// </summary>
        /// <param name="request">客戶群組資料。</param>
        /// <returns>儲存結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationCustomerGroup)]
        public JsonResult Save(ReconciliationCustomerGroupSaveRequest request)
        {
            try
            {
                _service.Save(request);
                return Json(new ResponseModel
                {
                    status = Status.success,
                    msg = request.Id.HasValue ? "修改成功" : "新增成功"
                });
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除客戶群組。
        /// </summary>
        /// <param name="id">客戶群組識別碼。</param>
        /// <returns>刪除結果。</returns>
        [HttpPost]
        [UserAuthorize(Authority.ReconciliationCustomerGroup)]
        public JsonResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return Json(new ResponseModel
                {
                    status = Status.success,
                    msg = "刪除成功"
                });
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

    }
}
