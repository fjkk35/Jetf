using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class CustWorkLoadReportViewModel
    {
        [Display(Name = "日　　期")]
        public string sDate { get; set; }
        [Display(Name = "日　　期")]
        public string eDate { get; set; }
        [Display(Name = "客　　戶")]
        public string custId { get; set; }

        [Display(Name = "客　　戶")]
        public IEnumerable<SelectListItem> ddlCustomerList { get; set; }
        [Display(Name = "客戶格式")]
        public string custTypeId { get; set; }
        [Display(Name = "客戶格式")]
        public IEnumerable<SelectListItem> ddlCustomerTypeList { get; set; }
    }
}