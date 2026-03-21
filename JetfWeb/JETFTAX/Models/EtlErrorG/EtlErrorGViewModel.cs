using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.EtlErrorG
{
    public class EtlErrorGViewModel
    {
        [Display(Name = "日　　期")]
        public string SDate { get; set; }

        [Display(Name = "日　　期")]
        public string EDate { get; set; }

        [Display(Name = "快遞專區綜合資料查詢")]
        public bool IsSearch { get; set; }
    }
}