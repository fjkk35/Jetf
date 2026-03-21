using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class LineGroupViewModel
    {
        [Display(Name = "群組代號")]
        public string GroupId { get; set; } 
        [Display(Name = "群組名稱")]
        public string GroupName { get; set; }
        public string Token { get; set; }

    }
}