using JETFTAX.Models.WorkDay;
using Service.EnumTax;
using Service.Services;
using Service.Services.WorkDay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JETFTAX.Extensions;
using Service.Models;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class WorkDayController : Controller
    {
        private readonly WorkDayService _workDayService;

        public WorkDayController(WorkDayService workDayService) 
        {
            _workDayService = workDayService;
        }

        public ActionResult Index()
        {
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);

            WorkDayViewModel vm = new WorkDayViewModel
            { 
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = startDate.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd")
            };

            return View(vm);
        }

        [HttpPost]
        public ActionResult Index(WorkDayViewModel vm)
        {
            DateTime.TryParse(vm.StartDate, out var startDate);
            DateTime.TryParse(vm.EndDate, out var endDate);

            var list = _workDayService.GetDate(startDate, endDate);

           vm.DateList = list;

           return View(vm);
        }

        [HttpPost]
        [UserAuthorize(Authority.WorkDay)]
        public ActionResult UpdateType(DateTime date,DateType type) 
        {
            var resopnseModel = _workDayService.UpdateType(date, type, UserContextService.GetUserId());

            return Json(resopnseModel, JsonRequestBehavior.AllowGet);
        }
    }
}