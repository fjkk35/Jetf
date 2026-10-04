using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PDTWebAPI.Models.Pdt
{
    public class LoginResponseModel
    {
        /// <summary>
        /// Call API 是否有成功
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// 是否須更新
        /// </summary>
        public string ResultCode { get; set; } = "";
        /// <summary>
        /// 訊息
        /// </summary>
        public string ResultMessage { get; set; } = "";
        /// <summary>
        /// 下載路徑
        /// </summary>
        public string Url { get; set; } = "";

        /// <summary>
        /// 派件公司清單
        /// </summary>
        public List<TransItem> TransList { get; set; }

        public List<DataTypeItem> DataTypeList { get; set; }
    }

    public class TransItem
    {
        /// <summary>
        /// 派件公司代號
        /// </summary>
        public string TransNo { get; set; }
        /// <summary>
        /// 派件公司名稱
        /// </summary>
        public string TransName { get; set; }
    }

    public class DataTypeItem
    {
        /// <summary>
        /// 作業區域
        /// </summary>
        public string DataType { get; set; }
    }
}