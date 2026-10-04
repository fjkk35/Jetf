using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PDTWebAPI.Models.Pdt
{
    public class LoginModel
    {
        /// <summary>
        /// 作業人員
        /// </summary>
        [Required(ErrorMessage = "UploadOpe is null")]
        public string UploadOpe { get; set; }
        /// <summary>
        /// 作業時間
        /// </summary>
        [Required(ErrorMessage = "UploadTime is null")]
        public string UploadTime { get; set; }
        /// <summary>
        /// 現行版號
        /// </summary>
        [Required(ErrorMessage = "Version is null")]
        public string Version { get; set; }

        
    }
}