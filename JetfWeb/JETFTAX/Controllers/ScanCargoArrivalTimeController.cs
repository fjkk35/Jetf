using JETFTAX.Models.CCLWork;
using JETFTAX.Models.ScanCargoArrivalTime;
using Newtonsoft.Json;
using Service.Models;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Controllers
{
    public class ScanCargoArrivalTimeController : Controller
    {
        private readonly CCLWorkService _cclWorkService;
        private readonly ScanCargoArrivalTimeService _scanCargoArrivalTimeService;

        public ScanCargoArrivalTimeController(CCLWorkService cclWorkService, ScanCargoArrivalTimeService scanCargoArrivalTimeService) 
        {
            _cclWorkService = cclWorkService;
            _scanCargoArrivalTimeService = scanCargoArrivalTimeService;
        }

        // GET: ScanCargoArrivalTime
        public ActionResult Index()
        {
            ScanCargoArrivalTimeViewModel vm = new ScanCargoArrivalTimeViewModel();
            DateTime date = DateTime.Now;
            vm.SDate = $"{date.ToString("yyyy-MM-dd")} 00:00";
            vm.EDate = $"{date.ToString("yyyy-MM-dd")} 23:59";
            DataTable dt_DataType = _cclWorkService.GetPdtDataType();
            List<SelectListItem> dataTypeList = new List<SelectListItem>();
            foreach (DataRow item in dt_DataType.Rows)
            {
                dataTypeList.Add(new SelectListItem() { Text = item["DataType"].ToString(), Value = item["DataType"].ToString() });
            }
            vm.DataTypeList = dataTypeList;

            DataTable dt_Trans = _cclWorkService.GetPdtTrans();
            List<SelectListItem> transList = new List<SelectListItem>();
            foreach (DataRow item in dt_Trans.Rows)
            {
                transList.Add(new SelectListItem() { Text = item["TransName"].ToString(), Value = item["TransNo"].ToString() });
            }
            vm.TransList = transList;
            return View(vm);
        }

        [HttpPost]
        public ActionResult Index(ScanCargoArrivalTimeViewModel vm) 
        {
            var result = _scanCargoArrivalTimeService.GetData(vm.Trans, vm.DataType, vm.SDate, vm.EDate,Session["user_id"].ToString());

            string data = JsonConvert.SerializeObject(result);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateScanCargoArrivalTime(string arrivalTime,string transName, string searchTime, string searchOpe)
        {
            var result = new ResopnseModel();
            if (DateTime.TryParse(arrivalTime, out var dateTime))
            {
                result = _scanCargoArrivalTimeService.UpdateScanCargoArrivalTime(dateTime.ToString("yyyy-MM-dd HH:mm:ss"), transName, searchTime, searchOpe);
            }
            else {
                result.status = Status.error;
                result.msg = "交艙時間錯誤";
            }
          
            string data = JsonConvert.SerializeObject(result);
            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}