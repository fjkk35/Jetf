using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.Customer.Domain;
using System;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerService _customerService;

        public CustomerController(CustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// 客戶查詢
        /// </summary>
        /// <returns></returns>
        //[UserAuthorize("1","2")]
        [UserAuthorize(Authority.SearchCustomer)]
        public ActionResult SearchCustomer()
        {
            return View();
        }

        [HttpGet]
        [UserAuthorize(Authority.SearchCustomer)]
        public JsonResult GetFormOptions()
        {
            try
            {
                var result = _customerService.GetFormOptions();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [UserAuthorize(Authority.SearchCustomer)]
        public JsonResult GetCustomerOptions(string tranType)
        {
            try
            {
                var result = _customerService.GetCustomerOptions(tranType);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.SearchCustomer)]
        public JsonResult QueryCustomers(CustomerQueryRequest request)
        {
            try
            {
                var result = _customerService.QueryCustomers(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.SearchCustomer)]
        public ActionResult ExportExcel(CustomerQueryRequest request)
        {
            try
            {
                var fileBytes = _customerService.ExportExcel(request);
                string fileName = $"客戶查詢_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [UserAuthorize(Authority.SearchCustomer)]
        public JsonResult GetCustomerDetail(int id)
        {
            try
            {
                var result = _customerService.GetCustomerDetail(id);
                if (result == null)
                {
                    return Json(new ResponseModel("查無客戶資料"), JsonRequestBehavior.AllowGet);
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [UserAuthorize(Authority.SearchCustomer)]
        public JsonResult SaveCustomer(CustomerUpsertModel request)
        {
            try
            {
                string userId = Session["user_id"]?.ToString() ?? "system";
                var result = _customerService.SaveCustomer(request, userId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel(ex.Message));
            }
        }
    }
}
