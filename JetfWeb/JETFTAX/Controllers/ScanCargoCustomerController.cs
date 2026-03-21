using iTextSharp.text;
using iTextSharp.text.pdf;
using JETFTAX.Models.CCLWork;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Service.EnumTax;
using Service.Models;
using Service.Services;
using Service.Services.ScanCargoCustomer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class ScanCargoCustomerController : Controller
    {
        CCLWorkService cCLWorkService = new CCLWorkService();

        private readonly ScanCargoCustomerService _scanCargoCustomerService;
        public ScanCargoCustomerController(ScanCargoCustomerService scanCargoCustomerService) 
        {
            _scanCargoCustomerService = scanCargoCustomerService;
        }
        
        /// <summary>
        /// 掃貨上車交接客戶派件公司明細表
        /// </summary>
        /// <returns></returns>
        [UserAuthorize(Authority.ScanCargoCustomerDetails)]
        public ActionResult ScanCargoCustomerDetails()
        {
            ScanCargoDetailsViewModel vm = new ScanCargoDetailsViewModel();
            DateTime date = DateTime.Now;
            vm.sDate = $"{date.ToString("yyyy-MM-dd")} 00:00";
            vm.eDate = $"{date.ToString("yyyy-MM-dd")} 23:59";
            DataTable dt_DataType = cCLWorkService.GetPdtDataType();
            List<SelectListItem> dataTypeList = new List<SelectListItem>();
            foreach (DataRow item in dt_DataType.Rows)
            {
                dataTypeList.Add(new SelectListItem() { Text = item["DataType"].ToString(), Value = item["DataType"].ToString() });
            }
            vm.ddlDataTypeList = dataTypeList;

            DataTable dt_Trans = cCLWorkService.GetPdtTrans();
            List<SelectListItem> transList = new List<SelectListItem>();
            foreach (DataRow item in dt_Trans.Rows)
            {
                transList.Add(new SelectListItem() { Text = item["TransName"].ToString(), Value = item["TransNo"].ToString() });
            }
            vm.ddlTransList = transList;
            return View(vm);
        }

        /// <summary>
        /// 掃貨上車交接客戶派件公司明細表-PDF
        /// </summary>
        /// <param name="vm"></param>
        /// <returns></returns>
        [UserAuthorize(Authority.ScanCargoCustomerDetails)]
        public ActionResult ScanCargoCustomerDetailsPdf(ScanCargoDetailsViewModel vm)
        {
            ResopnseModel resopnseModel = new ResopnseModel();

            //取得資料
            var result = _scanCargoCustomerService.GetScanCargoCustomerDetailsPdf(vm.trans, vm.dataType, vm.sDate, vm.eDate);

            DataTable dt = result.Item1;

            if (dt.Rows.Count > 0)
            {
                string dataDate = Convert.ToDateTime(vm.eDate).ToString("yyyy/MM/dd");
                //pdt 派件公司
                string pdfTrans = vm.trans;
                byte[] content = _scanCargoCustomerService.GetCustomerPdf(dt, dataDate, pdfTrans, vm.dataType);
                Response.AppendHeader("Content-Disposition", "inline; filename=掃貨上車.pdf;");
                return File(content, "application/pdf");
            }
            else
            {
                return Content("查無資料");
            }
        }
    }
}