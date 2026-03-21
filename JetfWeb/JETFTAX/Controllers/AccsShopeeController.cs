using Service.EnumTax;
using Service.Models;
using Service.Services.AccsShopee;
using Service.Services.AccsShopee.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class AccsShopeeController : Controller
    {
        private readonly AccsShopeeService _accsShopeeService;

        public AccsShopeeController(AccsShopeeService accsShopeeService)
        {
            _accsShopeeService = accsShopeeService;
        }

        // GET: AccsShopee
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得驗證碼圖片
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<JsonResult> GetVerifyCode()
        {
            try
            {
                var result = await _accsShopeeService.GetVerifyCodeImageAsync();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 登入 Accs 系統
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> Login(AccsLoginRequest request)
        {
            try
            {
                var result = await _accsShopeeService.LoginAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message));
            }
        }


        /// <summary>
        /// 匯出 Excel
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> ExportExcel(AccsQueryRequest request)
        {
            try
            {
                var workbook = await _accsShopeeService.ExportExcel(request);

                string handle = Guid.NewGuid().ToString();
                string fileName = $"Accs關貿空運查詢_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }

                return new JsonResult()
                {
                    Data = new { fileGuid = handle, fileName = fileName, msg = "" }
                };
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel($"匯出失敗：{ex.Message}"));
            }
        }

    }
}