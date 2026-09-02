using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services.TaxJob.Model
{
    /// <summary>
    /// 捷利稅金 API 的回覆資料。
    /// </summary>
    public class TaxResponseModel
    {
        /// <summary>
        /// 對應的 FEE_MASTER 流水號。
        /// </summary>
        public int FeeMasterId { get; set; }

        /// <summary>
        /// API 回覆代碼。
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// API 回覆資料旗標。
        /// </summary>
        public bool Data { get; set; }

        /// <summary>
        /// API 回覆訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// API 處理狀態。
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// API 回覆時間。
        /// </summary>
        public string Time { get; set; }
    }
}
