using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.SeaClearanceSjlTaxPayment
{
    public class SeaClearanceSjlTaxPaymentModel
    {
        public int Id { get; set; }

        public string Importer { get; set; }

        public string TaxPaymentDisplay { get; set; }

        public TaxPaymentType TaxPayment { get; set; }
    }
}
