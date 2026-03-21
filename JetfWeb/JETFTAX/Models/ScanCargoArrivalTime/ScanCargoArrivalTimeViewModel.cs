using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.ScanCargoArrivalTime
{
    public class ScanCargoArrivalTimeViewModel
    {
        [Display(Name = "日　　期")]
        public string SDate { get; set; }

        [Display(Name = "日　　期")]
        public string EDate { get; set; }

        [Display(Name = "作業地區")]
        public string DataType { get; set; }

        [Display(Name = "作業地區")]
        public IEnumerable<SelectListItem> DataTypeList { get; set; }

        [Display(Name = "派件公司")]
        public string Trans { get; set; }

        [Display(Name = "派件公司")]
        public IEnumerable<SelectListItem> TransList { get; set; }
    }
}