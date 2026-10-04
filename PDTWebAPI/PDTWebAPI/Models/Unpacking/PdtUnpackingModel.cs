using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PDTWebAPI.Models.Unpacking
{
    public class PdtUnpackingModel
    {
        /// <summary>
        /// API名稱
        /// </summary>
        public string API { get; set; }
        /// <summary>
        /// 作業地區
        /// </summary>
        public string DataType { get; set; }
        /// <summary>
        /// 袋號
        /// </summary>
        public string BagNo { get; set; }
        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }
        /// <summary>
        /// 作業人員
        /// </summary>
        public string UploadOpe { get; set; }
        /// <summary>
        /// 作業時間
        /// </summary>
        public string UploadTime { get; set; }
    }
}