using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Jetf
{
    public class TaxNumberPdfResponseModel
    {
        /// <summary>
        /// Call API 是否有成功
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 成功或失敗
        /// </summary>
        public string ResultCode { get; set; } = "";
        /// <summary>
        /// 訊息
        /// </summary>
        public string ResultMessage { get; set; } = "";

        public string TaxNumber { get; set; }

        public string Url { get; set; }
    }
}