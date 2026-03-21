using JETFTAX.Models.WorkLoad;
using NPOI.SS.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.EtlErrorWork;
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
    public class EtlErrorWorkController : Controller
    {
        private readonly EtlErrorWorkService _etlErrorWorkService;
        private readonly DropDownListService _dropDownListService;

        public EtlErrorWorkController(EtlErrorWorkService etlErrorWorkService, DropDownListService dropDownListService)
        {
            _etlErrorWorkService = etlErrorWorkService;
            _dropDownListService = dropDownListService;
        }

        // GET: EtlErrorWork
        [UserAuthorize(Authority.DownloadEtlErrorReport)]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得客戶群組列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.DownloadEtlErrorReport)]
        public JsonResult GetCustomerGroupList()
        {
            try
            {
                var groupList = _etlErrorWorkService.GetCustomerGroupList();
                return Json(groupList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 取得客戶群組明細
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        [HttpGet]
        [UserAuthorize(Authority.DownloadEtlErrorReport)]
        public JsonResult GetCustomerGroupDetail(int groupId)
        {
            try
            {
                var custCodes = _etlErrorWorkService.GetCustomerGroupDetail(groupId);
                return Json(custCodes, JsonRequestBehavior.AllowGet);
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
        [UserAuthorize(Authority.DownloadEtlErrorReport)]
        public JsonResult GetAllCustomerGroupDetails()
        {
            try
            {
                var allDetails = _etlErrorWorkService.GetAllCustomerGroupDetails();
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
        [HttpGet]
        [UserAuthorize(Authority.DownloadEtlErrorReport)]
        public JsonResult GetCustomerList()
        {
            try
            {
                var customerList = _dropDownListService.GetAirCustomerList().ToList();

                customerList.Add(new SelectListItem()
                {
                    Value = "無客戶",
                    Text = "無客戶"
                });

                return Json(customerList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new ResopnseModel(ex.Message), JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// 空快錯單作業-下載Excel
        /// </summary>
        /// <param name="sDate"></param>
        /// <param name="eDate"></param>
        /// <param name="custNames"></param>
        /// <returns></returns>
        [HttpPost]
        [UserAuthorize(Authority.DownloadEtlErrorReport)]
        public JsonResult DownloadExcel(string sDate, string eDate, List<string> custNames)
        {
            string fileName = "";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = _etlErrorWorkService.GenerateEtlErrorWorkWorkbookMultiple(custNames, sDate, eDate);

                if (workbook.NumberOfSheets == 0)
                {
                    workbook.CreateSheet("工作表1");
                }
                
                fileName = $"{sDate}~{eDate}-空快錯單作業.xlsx";
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

            return new JsonResult()
            {
                Data = new { fileGuid = handle, fileName = fileName, msg = msg }
            };
        }
    }
}