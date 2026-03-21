using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearanceCustomer;
using Service.Services.SeaClearanceCustomer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaClearanceCustomerController : Controller
    {
        private readonly SeaClearanceCustomerService _seaClearanceCustomerService;

        public SeaClearanceCustomerController(SeaClearanceCustomerService seaClearanceCustomerService)
        {
            _seaClearanceCustomerService = seaClearanceCustomerService;
        }

        // GET: SeaClearanceCustomer
        [UserAuthorize(Authority.SeaClearanceCustomer)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得所有可用的客戶列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.SeaClearanceCustomer)]
        public JsonResult GetAvailableCustomers()
        {
            try
            {
                var result = _seaClearanceCustomerService.GetAvailableCustomers();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得已選擇的客戶列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetSelectedCustomers()
        {
            try
            {
                var result = _seaClearanceCustomerService.GetSelectedCustomers();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 批量新增客戶
        /// </summary>
        /// <param name="customerCodes">客戶代碼列表</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AddCustomers(List<string> customerCodes)
        {
            try
            {
                var result = _seaClearanceCustomerService.AddCustomers(customerCodes);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量刪除客戶
        /// </summary>
        /// <param name="customerCodes">客戶代碼列表</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult DeleteCustomers(List<string> customerCodes)
        {
            try
            {
                var result = _seaClearanceCustomerService.DeleteCustomers(customerCodes);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 批量操作（新增或刪除）
        /// </summary>
        /// <param name="model">批量操作模型</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult BatchOperation(CustomerBatchOperationModel model)
        {
            try
            {
                if (model.Operation == "Add")
                {
                    var result = _seaClearanceCustomerService.AddCustomers(model.CustomerCodes);
                    return Json(result);
                }
                else if (model.Operation == "Delete")
                {
                    var result = _seaClearanceCustomerService.DeleteCustomers(model.CustomerCodes);
                    return Json(result);
                }
                else
                {
                    return Json(new ResopnseModel("不支援的操作類型"));
                }
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }
    }
}