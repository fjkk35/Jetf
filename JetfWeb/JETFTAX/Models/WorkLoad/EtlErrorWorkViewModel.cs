using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.WorkLoad
{
    public class EtlErrorWorkViewModel
    {
        [Display(Name = "日　　期")]
        public string sDate { get; set; }
        [Display(Name = "日　　期")]
        public string eDate { get; set; }
        [Display(Name = "客　　戶")]
        public string custName { get; set; }
        [Display(Name = "客　　戶")]
        public IEnumerable<SelectListItem> ddlCustomerList { get; set; }
    }
}