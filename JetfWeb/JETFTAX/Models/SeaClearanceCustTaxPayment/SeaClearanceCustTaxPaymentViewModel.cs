using Service.EnumTax;
using Service.Models.SeaClearanceCustTaxPayment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.SeaClearanceCustTaxPayment
{
    public class SeaClearanceCustTaxPaymentViewModel
    {

        public string CustCode { get; set; }

        public IEnumerable<SelectListItem> CustList { get; set; }

        public TaxPaymentType TaxPaymentType { get; set; }

        public IEnumerable<SelectListItem> TaxPaymentTypeList { get; set; }

        public List<SeaClearanceCustTaxPaymentModel> List { get; set; }
    }
}