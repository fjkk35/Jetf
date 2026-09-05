using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JETFWebAPI.Models.Jetf
{
    public class TaxNumberResponseModel
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

        public string TrackingNo { get; set; }

        public List<TaxNumberItem> TaxNumberList { get; set; }
    }

    public class TaxNumberItem
    {
        public string TaxNumber { get; set; }
    }
}