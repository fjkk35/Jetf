using Service.EnumTax;
using Service.Helpers;
using Service.Models;
using Service.Models.ApprovalCategory;
using Service.Services.ApprovalCategory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ApprovalCategoryController : Controller
    {
        private readonly ApprovalCategoryService _approvalCategoryService;

        public ApprovalCategoryController(ApprovalCategoryService approvalCategoryService)
        {
            _approvalCategoryService = approvalCategoryService;
        }

        // GET: ApprovalCategory
        [UserAuthorize(Authority.ApprovalCategory)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有簽審類別
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAll()
        {
            try
            {
                var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30) };
                var result = CacheHelper.GetOrAdd(CacheName.GetApprovalCategory.ToString(),
                    () => _approvalCategoryService.GetAll(), policy);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 根據ID取得簽審類別
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetById(int id)
        {
            try
            {
                var result = _approvalCategoryService.GetById(id);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增簽審類別
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.ApprovalCategory)]
        public JsonResult Create(ApprovalCategoryModel model)
        {
            try
            {
                var result = _approvalCategoryService.Create(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetApprovalCategory.ToString());

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新簽審類別
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.ApprovalCategory)]
        public JsonResult Update(ApprovalCategoryModel model)
        {
            try
            {
                var result = _approvalCategoryService.Update(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetApprovalCategory.ToString());

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除簽審類別
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.ApprovalCategory)]
        public JsonResult Delete(int id)
        {
            try
            {
                var result = _approvalCategoryService.Delete(id);

                //移除快取
                CacheHelper.Remove(CacheName.GetApprovalCategory.ToString());

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量更新排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.ApprovalCategory)]
        public JsonResult UpdateSorts(List<ApprovalCategoryModel> sortUpdates)
        {
            try
            {
                var result = _approvalCategoryService.UpdateSorts(sortUpdates);

                //移除快取
                CacheHelper.Remove(CacheName.GetApprovalCategory.ToString());

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}