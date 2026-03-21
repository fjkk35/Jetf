using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class DownloadProcessViewModel
    {
        [Display(Name = "日　　期")]
        public string sDate { get; set; }

        [Display(Name = "日　　期")]
        public string eDate { get; set; }

        [Display(Name = "客　　戶")]
        public string custId { get; set; }

        [Display(Name = "客　　戶")]
        public IEnumerable<SelectListItem> ddlCustomerList { get; set; }

        [Display(Name = "分　　類")]
        public string ProcessType { get; set; }

        [Display(Name = "分　　類")]
        public IEnumerable<SelectListItem> ddlProcessTypeList { get; set; }

        [Display(Name = "結　　案")]
        public string Finish { get; set; }

        [Display(Name = "結　　案")]
        public IEnumerable<SelectListItem> ddlFinistList { get; set; }


    }
}