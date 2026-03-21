using JETFTAX.Models.ErrorOrderSendDetail;
using Service.Services.ErrorOrderSendDetail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class ErrorOrderSendDetailController : Controller
    {
        private readonly ErrorOrderSendDetailService _errorOrderSendDetailService;

        public ErrorOrderSendDetailController(ErrorOrderSendDetailService errorOrderSendDetailService)
        {
            _errorOrderSendDetailService = errorOrderSendDetailService;
        }

        public ActionResult Index()
        {
            var vm = new ErrorOrderSendDetailViewModel() {
                StartDate = DateTime.Now.ToString("yyyy-MM-dd"),
                EndDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            return View(vm);
        }

        public ActionResult Query(ErrorOrderSendDetailViewModel vm) 
        {
            var resopnse = _errorOrderSendDetailService.GetErrorOrderSendDetail(vm.StartDate, vm.EndDate, vm.TrackingNo);

            return Json(resopnse, JsonRequestBehavior.AllowGet);
        }
    }
}