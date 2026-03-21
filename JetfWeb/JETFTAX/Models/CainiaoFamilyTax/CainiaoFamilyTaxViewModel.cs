using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.CainiaoFamilyTax
{
    public class CainiaoFamilyTaxViewModel
    {
        [Display(Name = "出倉日　")]
        public string StartDate { get; set; }

        [Display(Name = "出倉日　")]
        public string EndDate { get; set; }

        [Display(Name = "資料區間")]
        public IEnumerable<SelectListItem> DateTimeList { get; set; }

        [Display(Name = "客　　戶")]
        public EtlFamilyTax Customer { get; set; }

        [Display(Name = "客　　戶")]
        public IEnumerable<SelectListItem> CustomerList { get; set; }
    }
}