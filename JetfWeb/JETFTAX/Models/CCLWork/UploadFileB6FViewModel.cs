using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models.CCLWork
{
    public class UploadFileB6FViewModel
    {
        [Display(Name = "資料來源")]
        public string source { get; set; }
        [Display(Name = "資料來源")]
        public IEnumerable<SelectListItem> ddlSourceList { get; set; }
    }
}