using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.ErrorOrderSendDetail
{
    public class ErrorOrderSendDetailViewModel
    {
        [Display(Name = "日　　期")]
        public string StartDate { get; set; }

        [Display(Name = "日　　期")]
        public string EndDate { get; set; }

        [Display(Name = "分提單號")]
        public string TrackingNo { get; set; }
    }
}