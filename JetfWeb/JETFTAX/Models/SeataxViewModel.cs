using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class SeataxViewModel
    {
        [Display(Name = "日　　期")]
        public string date { get; set; }
        public SeaTaxType taxType { get; set; }

        [Display(Name = "稅金種類")]
        public IEnumerable<SelectListItem> ddlTaxTypeList { get; set; }
    }
}