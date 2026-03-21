using Service.EnumTax;
using Service.Models;
using Service.Services.CustomerTaxSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CustomerTaxSettingController : Controller
    {
        private readonly CustomerTaxSettingService _customerTaxSettingService;

        public CustomerTaxSettingController(CustomerTaxSettingService customerTaxSettingService)
        {
            _customerTaxSettingService = customerTaxSettingService;
        }

        // GET: CustomerTaxSetting
        [UserAuthorize(Authority.CustomerTaxSetting)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得SEA客戶列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetSeaCustomers()
        {
            try
            {
                var result = _customerTaxSettingService.GetSeaCustomers();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得所有稅金時間
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.CustomerTaxSetting)]
        public JsonResult GetTaxTimes()
        {
            try
            {
                var result = _customerTaxSettingService.GetTaxTimes();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得客戶稅金時間設定列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.CustomerTaxSetting)]
        public JsonResult GetCustomerTaxSettings()
        {
            try
            {
                var result = _customerTaxSettingService.GetCustomerTaxSettings();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得特定客戶的稅金時間設定
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.CustomerTaxSetting)]
        public JsonResult GetCustomerTaxSetting(string custCode)
        {
            try
            {
                var result = _customerTaxSettingService.GetCustomerTaxSetting(custCode);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 儲存客戶稅金時間設定
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <param name="taxTimeIds">稅金時間ID列表</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomerTaxSetting)]
        public JsonResult SaveCustomerTaxSetting(string custCode, List<int> taxTimeIds)
        {
            try
            {
                var result = _customerTaxSettingService.SaveCustomerTaxSetting(custCode, taxTimeIds);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }

        /// <summary>
        /// 刪除客戶稅金時間設定
        /// </summary>
        /// <param name="custCode">客戶代號</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomerTaxSetting)]
        public JsonResult DeleteCustomerTaxSetting(string custCode)
        {
            try
            {
                var result = _customerTaxSettingService.DeleteCustomerTaxSetting(custCode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }
    }
}