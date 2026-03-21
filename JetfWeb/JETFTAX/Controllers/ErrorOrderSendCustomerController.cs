using JETFTAX.Models.ErrorOrderSendCustomer;
using Service.Models;
using Service.Models.ErrorOrderSend;
using Service.Services.ErrorOrderSendCustomer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class ErrorOrderSendCustomerController : Controller
    {
        private readonly ErrorOrderSendCustomerService _errorOrderSendCustomerService;

        public ErrorOrderSendCustomerController(ErrorOrderSendCustomerService errorOrderSendCustomerService)
        {
            _errorOrderSendCustomerService = errorOrderSendCustomerService;
        }

        // GET: ErrorOrderSendCustomer
        public ActionResult Index()
        {
            var list = _errorOrderSendCustomerService.GetCustomerPlatformMapping();

            var vm = new ErrorOrderSendCustomerViewModel()
            {
                List = list.Select(r => new ErrorOrderSendCustomer
                {
                    Id = r.Id,
                    Customer = r.Customer,
                    Platform = r.Platform
                }).ToList()
            };

            return View(vm);
        }

        public ActionResult Create(ErrorOrderSendCustomer data) 
        {
            if (string.IsNullOrEmpty(data.Customer) || string.IsNullOrEmpty(data.Platform))
                return Json(new ResopnseModel("請輸入客戶、平台資料"), JsonRequestBehavior.AllowGet);

            var result = _errorOrderSendCustomerService.Create(new CustomerPlatformMapping
            {
                Customer = data.Customer,
                Platform = data.Platform
            });

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Delete(int id)
        {
            var result = _errorOrderSendCustomerService.Delete(id);

            return Json(result, JsonRequestBehavior.AllowGet);
        }



    }
}