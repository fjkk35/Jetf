using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace JETFTAX.Models
{
    public class AccountViewModel
    {
        [Required]
        [Display(Name = "帳號")]
        public string account { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        public string password { get; set; }


        [Required]
        [Display(Name = "驗證碼")]
        public string code { get; set; }
    }
}