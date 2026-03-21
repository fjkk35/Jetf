using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearanceCustTaxPayment
{
    public class SeaClearanceCustTaxPaymentModel
    {
        public int Id { get; set; }

        public string CustCode { get; set; }

        public string CustName { get; set; }

        public string TaxPaymentDisplay { get; set; }

        public TaxPaymentType TaxPayment { get; set; }
    }
}
