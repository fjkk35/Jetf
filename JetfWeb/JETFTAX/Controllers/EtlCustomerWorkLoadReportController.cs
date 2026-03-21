using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.EtlCustomerWorkLoadReport;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class EtlCustomerWorkLoadReportController : Controller
    {
        private readonly EtlCustomerWorkLoadReportService _etlCustomerWorkLoadReportService;
        private readonly DropDownListService _dropDownListService;

        public EtlCustomerWorkLoadReportController(EtlCustomerWorkLoadReportService etlCustomerWorkLoadReportService, DropDownListService dropDownListService)
        {
            _etlCustomerWorkLoadReportService = etlCustomerWorkLoadReportService;
            _dropDownListService = dropDownListService;
        }

        /// <summary>
        /// 空快客戶作業量報表頁面
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.EtlCustomerWorkLoadReport)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得客戶群組列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.EtlCustomerWorkLoadReport)]
        public JsonResult GetCustomerGroupList()
        {
            try
            {
                var groupList = _etlCustomerWorkLoadReportService.GetCustomerGroupList();
                return Json(groupList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得所有客戶群組明細 (一次撈完)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.EtlCustomerWorkLoadReport)]
        public JsonResult GetAllCustomerGroupDetails()
        {
            try
            {
                var allDetails = _etlCustomerWorkLoadReportService.GetAllCustomerGroupDetails();
                return Json(allDetails, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得客戶列表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.EtlCustomerWorkLoadReport)]
        public JsonResult GetCustomerList()
        {
            try
            {
                var customerList = _dropDownListService.GetAirCustomerList();

                return Json(customerList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 下載 Excel
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.EtlCustomerWorkLoadReport)]
        [HttpPost]
        public JsonResult DownloadExcel(DownloadExcelRequest model)
        {
            string fileName = $"{model.sDate}~{model.eDate}-空快客戶作業量報表.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";

            try
            {
                IWorkbook workbook = _etlCustomerWorkLoadReportService.GetCustWorkLoadReportWorkbookMultiple(
                    model.custIds,
                    model.custTypeId,
                    model.sDate,
                    model.eDate,
                    model.mainNumbers);

                using (MemoryStream fileStream = new MemoryStream())
                {
                    workbook.Write(fileStream);
                    TempData[handle] = fileStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            return Json(new { fileGuid = handle, fileName = fileName, msg = msg });
        }

        /// <summary>
        /// 下載 Excel 請求模型
        /// </summary>
        public class DownloadExcelRequest
        {
            public string sDate { get; set; }
            public string eDate { get; set; }
            public List<string> custIds { get; set; }
            public string custTypeId { get; set; }
            public List<string> mainNumbers { get; set; }
        }
    }
}