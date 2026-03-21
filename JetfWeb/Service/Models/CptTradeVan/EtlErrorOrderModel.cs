using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptTradeVan
{
    public class EtlErrorOrderModel
    {
        /// <summary>
        /// Cpt代碼
        /// </summary>
        public string CptNo { get; set; }

        /// <summary>
        /// 報單號碼
        /// </summary>
        public string ClearanceNo { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 狀態列
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// 事件發生日期(最新)
        /// </summary>
        public string IssueDate { get; set; }

        /// <summary>
        /// 時間
        /// </summary>
        public string IssueTime { get; set; }

        /// <summary>
        /// 錯誤原因代碼
        /// </summary>
        public string RejReasonCode { get; set; }

        /// <summary>
        ///  錯誤原因說明
        /// </summary>
        public string RejReasonDesc { get; set; }

        /// <summary>
        /// 其他處理狀況(依新-->舊)
        /// </summary>
        public string OtherProType { get; set; }
    }
}
