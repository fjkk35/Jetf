using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class CainiaoTaxEditViewModel
    {
        [Display(Name = "資料來源")]
        public string Source { get; set; }

        [Display(Name = "資料來源")]
        public IEnumerable<SelectListItem> ddlSourceList { get; set; }

        [Display(Name = "欄　　位")]
        public string Column { get; set; }

        [Display(Name = "欄　　位")]
        public IEnumerable<SelectListItem> ddlColumnList { get; set; }
    }
}