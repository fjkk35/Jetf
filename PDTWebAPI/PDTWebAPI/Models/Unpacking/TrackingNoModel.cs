using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PDTWebAPI.Models.Unpacking
{
    public class TrackingNoModel
    {
        /// <summary>
        /// 作業地區
        /// </summary>
        [Required(ErrorMessage = "DataType is null")]
        public string DataType { get; set; }
        /// <summary>
        /// 袋號
        /// </summary>
        [Required(ErrorMessage = "BagNo is null")]
        public string BagNo { get; set; }
        /// <summary>
        /// 分提單號
        /// </summary>
        [Required(ErrorMessage = "TrackingNo is null")]
        public string TrackingNo { get; set; }
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
    }
}