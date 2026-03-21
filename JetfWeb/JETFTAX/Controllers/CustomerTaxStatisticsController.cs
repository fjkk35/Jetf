using Service.EnumTax;
using Service.Models;
using Service.Services.CustomerTaxStatistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CustomerTaxStatisticsController : Controller
    {
        private readonly CustomerTaxStatisticsService _customerTaxStatisticsService;

        public CustomerTaxStatisticsController(CustomerTaxStatisticsService customerTaxStatisticsService)
        {
            _customerTaxStatisticsService = customerTaxStatisticsService;
        }

        // GET: CustomerTaxStatistics
        [UserAuthorize(Authority.CustomerTaxStatistics)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得客戶列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.CustomerTaxStatistics)]
        public JsonResult GetCustomers()
        {
            try
            {
                var result = _customerTaxStatisticsService.GetCustomers();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 匯出Excel
        /// </summary>
        /// <param name="customerCode">客戶代號</param>
        /// <param name="startDate">開始日期</param>
        /// <param name="endDate">結束日期</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomerTaxStatistics)]
        public JsonResult ExportExcel(string customerCode, string startDate, string endDate)
        {
            try
            {
                var result = _customerTaxStatisticsService.ExportExcel(customerCode, startDate, endDate);
                
                if (result.Success)
                {
                    string handle = Guid.NewGuid().ToString();
                    TempData[handle] = result.FileData;
                    
                    return Json(new { 
                        success = true, 
                        fileGuid = handle, 
                        fileName = result.FileName, 
                        recordCount = result.RecordCount,
                        message = result.Message 
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"匯出失敗：{ex.Message}" });
            }
        }
    }
}