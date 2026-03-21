using JETFTAX.Models;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Services;
using Service.Services.EtlCustWorkLoad;
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
    public class EtlCustWorkLoadController : Controller
    {
        EtlCustWorkLoadService _etlCustWorkLoadService;
        public EtlCustWorkLoadController(EtlCustWorkLoadService etlCustWorkLoadService) 
        {
            _etlCustWorkLoadService = etlCustWorkLoadService;
        }

        /// <summary>
        /// 空快客戶作業量報表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.EtlCustomerWorkLoadReport)]
        public ActionResult CustWorkLoadReport()
        {
            string custId, custName;
            //客戶
            CustomerService customerService = new CustomerService();
            DataTable dt_CustList = customerService.GetCustomerList();
            List<SelectListItem> customerList = new List<SelectListItem>();
            List<SelectListItem> customerTypeList = new List<SelectListItem>();
            for (int i = 0; i < dt_CustList.Rows.Count; i++)
            {
                custId = dt_CustList.Rows[i]["CUST_ID"].ToString().Trim();
                custName = $"{dt_CustList.Rows[i]["TRAN_TYPE"].ToString()}-{dt_CustList.Rows[i]["CUSTOMER"].ToString()}";
                if (custName.IndexOf("空運-") > -1)
                {
                    customerList.Add(new SelectListItem() { Text = custName, Value = custId });
                }
            }
            //客戶格式
            customerTypeList.Add(new SelectListItem() { Text = "博豐", Value = "1" });
            customerTypeList.Add(new SelectListItem() { Text = "蝦皮", Value = "2" });

            CustWorkLoadReportViewModel vm = new CustWorkLoadReportViewModel()
            {
                sDate = DateTime.Now.ToString("yyyy-MM-dd"),
                eDate = DateTime.Now.ToString("yyyy-MM-dd"),
                ddlCustomerList = customerList,
                ddlCustomerTypeList = customerTypeList
            };

            return View(vm);
        }

        /// <summary>
        /// 空快客戶作業量報表-Excel
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        //[UserAuthorize("1", "2", "3", "4", "6")]
        [UserAuthorize(Authority.EtlCustomerWorkLoadReport)]
        public ActionResult CustWorkLoadReportExcel(CustWorkLoadReportViewModel vm)
        {
            string sDate = vm.sDate;
            string eDate = vm.eDate;
            string custId = vm.custId;
            string custTypeId = vm.custTypeId;
            string fileName = $"{sDate}~{eDate}-空快客戶作業量報表.xlsx";
            string handle = Guid.NewGuid().ToString();
            string msg = "";
            IWorkbook workbook;
            try
            {
                workbook = _etlCustWorkLoadService.GetCustWorkLoadReportWorkbook(custId, custTypeId, sDate, eDate);
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