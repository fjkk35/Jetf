using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.CainiaoHiLifeTaxDetails
{
    public class CainiaoHiLifeTaxDetailsViewModel
    {
        [Display(Name = "上傳日期")]
        public string StartDate { get; set; }

        [Display(Name = "上傳日期")]
        public string EndDate { get; set; }
    }
}