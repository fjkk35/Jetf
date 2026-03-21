using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class SearchCargo2ViewModel
    {
        [Display(Name = "查詢")]
        public string searchType { get; set; }

        [Display(Name = "查詢")]
        public IEnumerable<SelectListItem> ddlSearchTypeList { get; set; }
    }
}