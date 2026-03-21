using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class DownloadNoTransferViewModel
    {
        [Display(Name = "未匯款日期")]
        public string sDate { get; set; }
        [Display(Name = "未匯款日期")]
        public string eDate { get; set; }
    }
}