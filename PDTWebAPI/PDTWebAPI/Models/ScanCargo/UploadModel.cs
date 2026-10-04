using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PDTWebAPI.Models.ScanCargo
{
    public class UploadModel
    {
        /// <summary>
        /// 作業地區
        /// </summary>
        [Required(ErrorMessage = "DataType is null")]
        public string DataType { get; set; }
        /// <summary>
        /// 派件公司代號
        /// </summary>
        [Required(ErrorMessage = "TransNo is null")]
        public string TransNo { get; set; }
        /// <summary>
        /// 袋號或分提單號
        /// </summary>
        [Required(ErrorMessage = "DataList is null")]
        public List<DataItem> DataList { get; set; }
        /// <summary>
        /// 車號
        /// </summary>
        public string CarNo { get; set; }
        /// <summary>
        /// 作業人員
        /// </summary>
        [Required(ErrorMessage = "UploadOpe is null")]
        public string UploadOpe { get; set; }
    }

    public class DataItem
    {
        /// <summary>
        /// 袋號或分提單號
        /// </summary>
        //[Required(ErrorMessage = "DataList.Data is null")]
        public string Data { get; set; }
        /// <summary>
        /// 作業時間
        /// </summary>
        //[Required(ErrorMessage = "DataList.UploadTime is null")]
        public string UploadTime { get; set; }
    }
}