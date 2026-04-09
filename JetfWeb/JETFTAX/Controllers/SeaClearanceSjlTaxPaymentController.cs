using JETFTAX.Models.SeaClearanceCustTaxPayment;
using JETFTAX.Models.SeaClearanceSjlTaxPayment;
using Service.EnumTax;
using Service.Models;
using Service.Models.SeaClearanceCreate;
using Service.Models.SeaClearanceCustTaxPayment;
using Service.Models.SeaClearanceSjlTaxPayment;
using Service.Services;
using Service.Services.SeaClearanceCustTaxPayment;
using Service.Services.SeaClearanceSjlTaxPayment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static JETFTAX.Controllers.AccountController;

namespace JETFTAX.Controllers
{
    public class SeaClearanceSjlTaxPaymentController : Controller
    {
        private readonly DropDownListService _dropDownListService;
        private readonly SeaClearanceSjlTaxPaymentService _seaClearanceSjlTaxPaymentService;

        public SeaClearanceSjlTaxPaymentController(DropDownListService dropDownListService, SeaClearanceSjlTaxPaymentService seaClearanceSjlTaxPaymentService) 
        {
            _dropDownListService = dropDownListService;
            _seaClearanceSjlTaxPaymentService = seaClearanceSjlTaxPaymentService;
        }

        // GET: SeaClearanceSjlTaxPayment
        [UserAuthorize(Authority.SeaClearanceSjlTaxPayment)]
        public ActionResult Index()
        {
            var vm = new SeaClearanceSjlTaxPaymentViewModel() 
            {
                List = _seaClearanceSjlTaxPaymentService.GetSeaClearanceSjlTaxPayment(),
                TaxPaymentTypeList = _dropDownListService.GetTaxPaymentTypeList(),
            };
            return View(vm);
        }

        [UserAuthorize(Authority.SeaClearanceSjlTaxPayment)]
        public ActionResult Create(SeaClearanceSjlTaxPaymentViewModel vm)
        {
            if(string.IsNullOrEmpty(vm.Importer))
                return Json(new ResponseModel("請輸入申報人"), JsonRequestBehavior.AllowGet);

            var result = _seaClearanceSjlTaxPaymentService.Create(new SeaClearanceSjlTaxPaymentModel
            {
                Importer = vm.Importer,
                TaxPayment = vm.TaxPaymentType
            });

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [UserAuthorize(Authority.SeaClearanceSjlTaxPayment)]
        public ActionResult Delete(int id)
        {
            var result = _seaClearanceSjlTaxPaymentService.Delete(id);

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}