using Service.EnumTax;
using Service.Helpers;
using Service.Models;
using Service.Models.Step;
using Service.Services.Step;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class StepController : Controller
    {
        private readonly StepService _stepService;

        public StepController(StepService stepService)
        {
            _stepService = stepService;
        }

        // GET: Step
        [UserAuthorize(Authority.Step)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有步驟（包含步驟詳細）
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAllStepsWithDetails()
        {
            try
            {
                var result = _stepService.GetAllStepsWithDetails();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得所有步驟（不包含詳細）
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAllSteps()
        {
            try
            {
                var policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(30) };
                var result = CacheHelper.GetOrAdd(CacheName.GetAllSteps.ToString(),
                    () => _stepService.GetAllSteps(), policy, false);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 根據ID取得步驟
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetById(int id)
        {
            try
            {
                var result = _stepService.GetById(id);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增步驟
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult CreateStep(StepModel model)
        {
            try
            {
                var result = _stepService.CreateStep(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新步驟
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult UpdateStep(StepModel model)
        {
            try
            {
                var result = _stepService.UpdateStep(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除步驟
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult DeleteStep(int id)
        {
            try
            {
                var result = _stepService.DeleteStep(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量更新步驟排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult UpdateStepSorts(List<StepSortUpdateModel> sortUpdates)
        {
            try
            {
                var result = _stepService.UpdateStepSorts(sortUpdates);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得步驟的所有詳細
        /// </summary>
        /// <param name="stepId"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetStepDetails(int stepId)
        {
            try
            {
                var result = _stepService.GetStepDetails(stepId);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增步驟詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult CreateStepDetail(StepDetailModel model)
        {
            try
            {
                var result = _stepService.CreateStepDetail(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新步驟詳細
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult UpdateStepDetail(StepDetailModel model)
        {
            try
            {
                var result = _stepService.UpdateStepDetail(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除步驟詳細
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult DeleteStepDetail(int id)
        {
            try
            {
                var result = _stepService.DeleteStepDetail(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量更新步驟詳細排序
        /// </summary>
        /// <param name="sortUpdates"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.Step)]
        public JsonResult UpdateStepDetailSorts(List<StepDetailSortUpdateModel> sortUpdates)
        {
            try
            {
                var result = _stepService.UpdateStepDetailSorts(sortUpdates);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }
    }
}