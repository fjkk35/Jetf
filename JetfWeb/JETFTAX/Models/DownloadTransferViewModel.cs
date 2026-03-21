using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class DownloadTransferViewModel
    {
        [Display(Name = "匯款日期")]
        public string date { get; set; }
    }
   
}