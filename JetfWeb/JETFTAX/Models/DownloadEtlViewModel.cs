using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace JETFTAX.Models
{
    public class DownloadEtlViewModel
    {

        [Display(Name = "日　　期")]
        public string date { get; set; }
        public string timeBetween { get; set; }
        [Display(Name = "資料區間")]
        public IEnumerable<SelectListItem> ddlTimeBetweenList { get; set; }
        public string company { get; set; }
        public string sTime { get; set; }
        public string eTime { get; set; }
    }
}