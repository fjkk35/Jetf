using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models.WorkLoad
{
    public class UploadFileSeaManifestViewModel
    {
        [Display(Name = "日　　期")]
        public string DataDate { get; set; }
    }
}