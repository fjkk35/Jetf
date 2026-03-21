using JETFTAX.Models.SeaClearanceCustTaxPayment;
using JETFTAX.Models.SeaClearanceFee;
using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearanceCustTaxPayment;
using Service.Services;
using Service.Services.SeaClearanceCustTaxPayment;
using Service.Services.SeaClearanceFee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaClearanceFeeController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly SeaClearanceFeeService _seaClearanceFeeService;

        public SeaClearanceFeeController(DropDownListService dropDownListService, SeaClearanceFeeService seaClearanceFeeService)
        {
            _dropDownListService = dropDownListService;
            _seaClearanceFeeService = seaClearanceFeeService;
        }

        // GET: SeaClearanceFee
        [UserAuthorize(Authority.SeaClearanceFee)]
        public ActionResult Index()
        {
            var vm = new SeaClearanceFeeViewModel() 
            {
                CustList = _dropDownListService.GetSeaCustomerList(),
                List = _seaClearanceFeeService.GetSeaClearanceFee()
            };
            return View(vm);
        }

        [UserAuthorize(Authority.SeaClearanceFee)]
        public ActionResult Create(SeaClearanceFeeViewModel vm)
        {
            var result = _seaClearanceFeeService.Create(new SeaClearanceFeeModel
            {
                CustCode = vm.CustCode,
                X2Fee = vm.X2Fee ?? 0,
                G1Fee = vm.G1Fee ?? 0,
                MoveWarehouseFee = vm.MoveWarehouseFee ?? 0,
                TransferG1Fee = vm.TransferG1Fee ?? 0,
                TransferWarehouseFee = vm.TransferWarehouseFee ?? 0
            });

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize(Authority.SeaClearanceFee)]
        public ActionResult Delete(int id)
        {
            var result = _seaClearanceFeeService.Delete(id);

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}