using Service.EnumTax;
using Service.Models;
using Service.Services.WorkDayArea;
using Service.Services.WorkDayArea.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class WorkDayAreaController : Controller
    {
        private readonly WorkDayAreaService _workDayAreaService;

        public WorkDayAreaController(WorkDayAreaService workDayAreaService)
        {
            _workDayAreaService = workDayAreaService;
        }

        /// <summary>
        /// 首頁
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得作業地區列表
        /// </summary>
        [HttpPost]
        public JsonResult GetWorkAreaList()
        {
            try
            {
                var result = _workDayAreaService.GetWorkAreaList();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 查詢工作天資料
        /// </summary>
        [HttpPost]
        public JsonResult Query(WorkDayAreaQueryRequest request)
        {
            try
            {
                var result = _workDayAreaService.QueryWorkDayArea(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        /// <summary>
        /// 更新工作天類型
        /// </summary>
        [HttpPost]
        [UserAuthorize(Authority.WorkDayArea)]
        public JsonResult UpdateWorkDayType(WorkDayAreaUpdateRequest request)
        {
            try
            {
                var result = _workDayAreaService.UpdateWorkDayType(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}