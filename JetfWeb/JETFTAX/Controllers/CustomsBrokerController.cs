using Service.EnumTax;
using Service.Models;
using Service.Models.CustomsBroker;
using Service.Services.CustomsBroker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CustomsBrokerController : Controller
    {
        private readonly CustomsBrokerService _customsBrokerService;

        public CustomsBrokerController(CustomsBrokerService customsBrokerService)
        {
            _customsBrokerService = customsBrokerService;
        }

        // GET: CustomsBroker
        [UserAuthorize(Authority.CustomsBroker)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得報驗公司列表 (AJAX)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult GetData(CustomsBrokerRequest request)
        {
            try
            {
                var result = _customsBrokerService.GetData(request);

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得單一報驗公司資料
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult GetById(int id)
        {
            try
            {
                var result = _customsBrokerService.GetById(id);
                return Json(new ResponseModel(result));
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 新增報驗公司
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult Insert(CustomsBrokerModel model)
        {
            try
            {
                // 驗證模型
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new ResponseModel(string.Join(", ", errors)));
                }

                var result = _customsBrokerService.Insert(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新報驗公司
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult Update(CustomsBrokerModel model)
        {
            try
            {
                // 驗證模型
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new ResponseModel(string.Join(", ", errors)));
                }

                // 這裡應該從 Session 或其他方式取得當前使用者ID
                var userId = Session["user_id"]?.ToString() ?? "system";
                
                var result = _customsBrokerService.Update(model, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除報驗公司
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult Delete(int id)
        {
            try
            {
                var userId = Session["user_id"]?.ToString() ?? "system";
                var result = _customsBrokerService.Delete(id, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 取得所有報驗公司 (用於下拉選單)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetAllForDropdown()
        {
            try
            {
                var result = _customsBrokerService.GetAllForDropdown();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得港區選項 (用於下拉選單)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetPortAreaList()
        {
            try
            {
                var portAreas = new List<object>
                {
                    new { Value = "台北港", Text = "台北港" },
                    new { Value = "高雄港", Text = "高雄港" }
                };
                return Json(portAreas, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        #region 聯絡人相關 API

        /// <summary>
        /// 取得單一聯絡人資料
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult GetContactById(int id)
        {
            try
            {
                var result = _customsBrokerService.GetContactById(id);
                return Json(new ResponseModel(result));
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 新增聯絡人
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult InsertContact(CustomsBrokerContactModel model)
        {
            try
            {
                // 驗證模型
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new ResponseModel(string.Join(", ", errors)));
                }

                var result = _customsBrokerService.InsertContact(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新聯絡人
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult UpdateContact(CustomsBrokerContactModel model)
        {
            try
            {
                // 驗證模型
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new ResponseModel(string.Join(", ", errors)));
                }

                var userId = Session["user_id"]?.ToString() ?? "system";
                var result = _customsBrokerService.UpdateContact(model, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除聯絡人
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomsBroker)]
        public JsonResult DeleteContact(int id)
        {
            try
            {
                var result = _customsBrokerService.DeleteContact(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        #endregion
    }
}