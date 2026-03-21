using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class IncomeReportViewModel
    {
        [Display(Name = "日　　期")]
        public string sDate { get; set; }
        [Display(Name = "日　　期")]
        public string eDate { get; set; }
        [Display(Name = "重新轉檔")]
        public string rdoSearchType { get; set; }

        public string source { get; set; }

        [Display(Name = "資料來源")]
        public IEnumerable<SelectListItem> ddlSourceList { get; set; }

    }
}