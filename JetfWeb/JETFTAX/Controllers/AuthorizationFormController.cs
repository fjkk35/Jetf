using Service.EnumTax;
using Service.Helpers;
using Service.Models;
using Service.Models.AuthorizationForm;
using Service.Services.AuthorizationForm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class AuthorizationFormController : Controller
    {
        private readonly AuthorizationFormService _authorizationFormService;

        public AuthorizationFormController(AuthorizationFormService authorizationFormService)
        {
            _authorizationFormService = authorizationFormService;
        }

        // GET: AuthorizationForm
        [UserAuthorize(Authority.AuthorizationForm)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有文件名稱
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAll()
        {
            try
            {
                var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30) };
                var result = CacheHelper.GetOrAdd(CacheName.GetAuthorizationForm.ToString(),
                    () => _authorizationFormService.GetAll(), policy);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 根據ID取得文件名稱
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetById(int id)
        {
            try
            {
                var result = _authorizationFormService.GetById(id);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增文件名稱
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AuthorizationForm)]
        public JsonResult Create(AuthorizationFormModel model)
        {
            try
            {
                var result = _authorizationFormService.Create(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetAuthorizationForm.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新文件名稱
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AuthorizationForm)]
        public JsonResult Update(AuthorizationFormModel model)
        {
            try
            {
                var result = _authorizationFormService.Update(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetAuthorizationForm.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量更新排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AuthorizationForm)]
        public JsonResult UpdateSorts(List<AuthorizationFormModel> sortUpdates)
        {
            try
            {
                var result = _authorizationFormService.UpdateSorts(sortUpdates);

                //移除快取
                CacheHelper.Remove(CacheName.GetAuthorizationForm.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }
    }
}