using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.CCLWork
{
    public class UnpackingDetailsViewModel
    {
        [Display(Name = "日　　期")]
        public string sDate { get; set; }
        [Display(Name = "日　　期")]
        public string eDate { get; set; }
        public string dataType { get; set; }
        [Display(Name = "作業地區")]
        public IEnumerable<SelectListItem> ddlDataTypeList { get; set; }
    }
}