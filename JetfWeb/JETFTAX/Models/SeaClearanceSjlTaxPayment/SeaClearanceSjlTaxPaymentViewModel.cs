using Service.EnumTax;
using Service.Models.SeaClearanceSjlTaxPayment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.SeaClearanceSjlTaxPayment
{
    public class SeaClearanceSjlTaxPaymentViewModel
    {
        /// <summary>
        /// 原單申報人
        /// </summary>
        public string Importer { get; set; }

        public TaxPaymentType TaxPaymentType { get; set; }


        public IEnumerable<SelectListItem> TaxPaymentTypeList { get; set; }


        public List<SeaClearanceSjlTaxPaymentModel> List { get; set; }
    }
}