using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.PostClearance
{
    public class PostClearanceViewModel
    {
        [Display(Name = "日　　期")]
        public string DataDate { get; set; }
    }
}