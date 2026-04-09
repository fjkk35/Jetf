using Service.EnumTax;
using Service.Helpers;
using Service.Models;
using Service.Models.AbnormalState;
using Service.Services.AbnormalState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class AbnormalStateController : Controller
    {
        private readonly AbnormalStateService _abnormalStateService;

        public AbnormalStateController(AbnormalStateService abnormalStateService)
        {
            _abnormalStateService = abnormalStateService;
        }

        // GET: AbnormalState
        [UserAuthorize(Authority.AbnormalState)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有異常狀態（包含異常狀態詳細）
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAllAbnormalStatesWithDetails()
        {
            try
            {
                var result = _abnormalStateService.GetAllAbnormalStatesWithDetails();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得所有異常狀態（不包含詳細）
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAllAbnormalStates()
        {
            try
            {
                var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30) };
                var result = CacheHelper.GetOrAdd(CacheName.GetAllAbnormalStates.ToString(),
                    () => _abnormalStateService.GetAllAbnormalStates(), policy);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 根據ID取得異常狀態
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetById(int id)
        {
            try
            {
                var result = _abnormalStateService.GetById(id);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增異常狀態
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AbnormalState)]
        public JsonResult CreateAbnormalState(AbnormalStateModel model)
        {
            try
            {
                var result = _abnormalStateService.CreateAbnormalState(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新異常狀態
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AbnormalState)]
        public JsonResult UpdateAbnormalState(AbnormalStateModel model)
        {
            try
            {
                var result = _abnormalStateService.UpdateAbnormalState(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除異常狀態
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AbnormalState)]
        public JsonResult DeleteAbnormalState(int id)
        {
            try
            {
                var result = _abnormalStateService.DeleteAbnormalState(id);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量更新異常狀態排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AbnormalState)]
        public JsonResult UpdateAbnormalStateSorts(List<AbnormalStateSortUpdateModel> sortUpdates)
        {
            try
            {
                var result = _abnormalStateService.UpdateAbnormalStateSorts(sortUpdates);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得異常狀態的所有詳細
        /// </summary>
        /// <param name="abnormalStateId"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAbnormalStateDetails(int abnormalStateId)
        {
            try
            {
                var result = _abnormalStateService.GetAbnormalStateDetails(abnormalStateId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增異常狀態詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CreateAbnormalStateDetail(AbnormalStateDetailModel model)
        {
            try
            {
                var result = _abnormalStateService.CreateAbnormalStateDetail(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新異常狀態詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult UpdateAbnormalStateDetail(AbnormalStateDetailModel model)
        {
            try
            {
                var result = _abnormalStateService.UpdateAbnormalStateDetail(model);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除異常狀態詳細
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AbnormalState)]
        public JsonResult DeleteAbnormalStateDetail(int id)
        {
            try
            {
                var result = _abnormalStateService.DeleteAbnormalStateDetail(id);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量更新異常狀態詳細排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.AbnormalState)]
        public JsonResult UpdateAbnormalStateDetailSorts(List<AbnormalStateDetailSortUpdateModel> sortUpdates)
        {
            try
            {
                var result = _abnormalStateService.UpdateAbnormalStateDetailSorts(sortUpdates);

                //移除快取
                CacheHelper.Remove(CacheName.GetAllAbnormalStates.ToString());
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}