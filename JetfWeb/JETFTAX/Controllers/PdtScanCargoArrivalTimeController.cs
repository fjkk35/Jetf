using JETFTAX.Models.PdtScanCargoArrivalTime;
using Service.EnumTax;
using Service.Models;
using Service.Services.PdtScanCargoArrivalTime;
using Service.Services.PdtScanCargoArrivalTime.Domain;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class PdtScanCargoArrivalTimeController : Controller
    {
        private readonly PdtScanCargoArrivalTimeService _service;

        public PdtScanCargoArrivalTimeController(PdtScanCargoArrivalTimeService service)
        {
            _service = service;
        }

        // GET: PdtScanCargoArrivalTime
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetDataTypeList()
        {
            try
            {
                var list = _service.GetDataTypeList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetTransList()
        {
            try
            {
                var list = _service.GetTransList();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SearchData(PdtScanCargoArrivalTimeSearchVm vm)
        {
            try
            {
                var result = _service.Search(new PdtScanCargoArrivalTimeRequest
                {
                    StartTime = vm.StartTime,
                    EndTime = vm.EndTime,
                    TransNo = vm.TransNo,
                    DataType = vm.DataType
                });

                return Json(new { Data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult UpdateArrivalTime(PdtScanCargoArrivalTimeUpdateVm vm)
        {
            try
            {
                if (vm == null || !vm.ArrivalTime.HasValue)
                {
                    return Json(new { status = Status.error, msg = "請輸入交倉時間" }, JsonRequestBehavior.AllowGet);
                }

                var res = _service.UpdateArrivalTime(vm.ArrivalTime.Value, vm.TransName, vm.Ids ?? new List<string>());

                return new JsonResult
                {
                    Data = new { status = res.status, msg = res.msg, result = res.ReturnObject },
                };
            }
            catch (Exception ex)
            {
                return Json(new { status = Status.error, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}