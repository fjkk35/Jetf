using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.CompanySeaTax
{
    public class CompanySeaTaxViewModel
    {
        [Display(Name = "日　　期")]
        public string DataDate { get; set; }

        public SeaTaxType TaxType { get; set; }

        [Display(Name = "稅金種類")]
        public IEnumerable<SelectListItem> TaxTypeList { get; set; }

        public string Company { get; set; }

        [Display(Name = "物流公司")]
        public IEnumerable<SelectListItem> CompanyList { get; set; }
    }
}