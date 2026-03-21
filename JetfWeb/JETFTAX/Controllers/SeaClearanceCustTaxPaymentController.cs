using JETFTAX.Models.ErrorOrderSendCustomer;
using JETFTAX.Models.SeaClearanceCustTaxPayment;
using Service.EnumTax;
using Service.Models;
using Service.Models.ErrorOrderSend;
using Service.Models.SeaClearanceCustTaxPayment;
using Service.Services;
using Service.Services.ErrorOrderSendCustomer;
using Service.Services.SeaClearanceCustTaxPayment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaClearanceCustTaxPaymentController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly SeaClearanceCustTaxPaymentService _seaClearanceCustTaxPaymentService;

        
        public SeaClearanceCustTaxPaymentController(DropDownListService dropDownListService, SeaClearanceCustTaxPaymentService seaClearanceCustTaxPaymentService)
        {
            _dropDownListService = dropDownListService;
            _seaClearanceCustTaxPaymentService = seaClearanceCustTaxPaymentService;
        }

        // GET: SeaClearanceCustTaxPayment
        [UserAuthorize(Authority.SeaClearanceCustTaxPayment)]
        public ActionResult Index()
        {
            var vm = new SeaClearanceCustTaxPaymentViewModel() 
            {
                List = _seaClearanceCustTaxPaymentService.GetSeaClearanceCustTaxPayment(),
                CustList = _dropDownListService.GetSeaCustomerList(),
                TaxPaymentTypeList = _dropDownListService.GetTaxPaymentTypeList()
            };
            
            return View(vm);
        }

        [UserAuthorize(Authority.SeaClearanceCustTaxPayment)]
        public ActionResult Create(SeaClearanceCustTaxPaymentViewModel vm)
        {
            var result = _seaClearanceCustTaxPaymentService.Create(new SeaClearanceCustTaxPaymentModel
            {
                CustCode = vm.CustCode,
                TaxPayment = vm.TaxPaymentType
            });

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize(Authority.SeaClearanceCustTaxPayment)]
        public ActionResult Delete(int id)
        {
            var result = _seaClearanceCustTaxPaymentService.Delete(id);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}