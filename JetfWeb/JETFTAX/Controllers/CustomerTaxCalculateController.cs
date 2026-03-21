using Service.EnumTax;
using Service.Models;
using Service.Services.CustomerTaxCalculate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class CustomerTaxCalculateController : Controller
    {
        private readonly CustomerTaxCalculateService _customerTaxCalculateService;

        public CustomerTaxCalculateController(CustomerTaxCalculateService customerTaxCalculateService)
        {
            _customerTaxCalculateService = customerTaxCalculateService;
        }

        // GET: CustomerTaxCalculate
        [UserAuthorize(Authority.CustomerTaxCalculate)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得稅金時間列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetTaxTimes()
        {
            try
            {
                var result = _customerTaxCalculateService.GetTaxTimes();
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
        /// <param name="taxTimeId">稅金時間ID</param>
        /// <param name="selectedDate">選擇日期</param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.CustomerTaxCalculate)]
        public ActionResult ExportExcel(int taxTimeId, string selectedDate)
        {
            try
            {
                if (taxTimeId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "請選擇稅金時間"
                    });
                }

                if (string.IsNullOrEmpty(selectedDate))
                {
                    return Json(new
                    {
                        success = false,
                        message = "請選擇日期"
                    });
                }

                if (!DateTime.TryParse(selectedDate, out DateTime date))
                {
                    return Json(new
                    {
                        success = false,
                        message = "日期格式錯誤"
                    });
                }

                var result = _customerTaxCalculateService.ExportExcel(taxTimeId, date);

                if (result.Success)
                {
                    // 將檔案內容存入 TempData 供下載使用
                    string handle = Guid.NewGuid().ToString();
                    TempData[handle] = result.FileData;

                    return Json(new
                    {
                        success = true,
                        fileGuid = handle,
                        fileName = result.FileName,
                        recordCount = result.RecordCount
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"匯出失敗：{ex.Message}"
                });
            }
        }
    }
}