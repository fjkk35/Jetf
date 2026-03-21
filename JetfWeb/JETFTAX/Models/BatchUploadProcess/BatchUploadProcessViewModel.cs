using Service.EnumTax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.BatchUploadProcess
{
    public class BatchUploadProcessViewModel
    {
        [Display(Name = "類別")]
        public Service.EnumTax.ProcessStatus Status { get; set; }

        [Display(Name = "類別")]
        public IEnumerable<SelectListItem> ProcessTypeList { get; set; }
    }
}