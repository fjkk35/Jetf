using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.WorkLoad
{
    public class UploadFileSeaBagNoViewModel
    {
        [Display(Name = "日　　期")]
        public string DataDate { get; set; }

    }
}