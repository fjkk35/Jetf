using Service.EnumTax;
using Service.Models;
using Service.Services.SeaClearanceProcessor;
using Service.Services.SeaClearanceProcessor.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaClearanceProcessorController : Controller
    {
        private readonly SeaClearanceProcessorService _seaClearanceProcessorService;

        public SeaClearanceProcessorController(SeaClearanceProcessorService seaClearanceProcessorService) 
        {
            _seaClearanceProcessorService = seaClearanceProcessorService;
        }

        // GET: SeaClearanceProcessor
        [UserAuthorize(Authority.SeaClearanceProcessor)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 查詢負責人列表
        /// </summary>
        /// <param name="query">查詢條件</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetProcessorList(SeaClearanceProcessorQueryModel query)
        {
            try
            {
                var result = _seaClearanceProcessorService.GetProcessorList(query);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 根據ID取得負責人資料
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetById(int id)
        {
            try
            {
                var result = _seaClearanceProcessorService.GetById(id);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 新增負責人
        /// </summary>
        /// <param name="model">負責人資料</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearanceProcessor)]
        public JsonResult CreateProcessor(SeaClearanceProcessorRequestModel model)
        {
            try
            {
                var result = _seaClearanceProcessorService.CreateProcessor(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新負責人
        /// </summary>
        /// <param name="model">負責人資料</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearanceProcessor)]
        public JsonResult UpdateProcessor(SeaClearanceProcessorRequestModel model)
        {
            try
            {
                var result = _seaClearanceProcessorService.UpdateProcessor(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除負責人
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.SeaClearanceProcessor)]
        public JsonResult DeleteProcessor(int id)
        {
            try
            {
                var result = _seaClearanceProcessorService.DeleteProcessor(id);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}