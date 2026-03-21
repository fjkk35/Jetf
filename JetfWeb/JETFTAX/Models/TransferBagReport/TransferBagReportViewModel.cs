using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.TransferBagReport
{
    public class TransferBagReportViewModel
    {
        [Display(Name = "日　　期")]
        public string StartDate { get; set; }

        [Display(Name = "日　　期")]
        public string EndDate { get; set; }
    }
}