using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Models.CptSeaMainNumberJob
{
    public class CptSeaMainNumberDetailModel
    {
        public int Id { get; set; }

        public string MainNumber { get; set; }

        public string BagNumber { get; set; }

        /// <summary>
        /// GB321是否連線收單建檔
        /// </summary>
        public bool IsReceiveOrder { get; set; }

        /// <summary>
        /// Gb321查詢狀態
        /// </summary>
        public string Gb321Status { get; set; }

        /// <summary>
        /// Gb321訊息
        /// </summary>
        public string Gb321Msg { get; set; }

        /// <summary>
        /// Gb321時間(最新)
        /// </summary>
        public string Gb321ProDateTime { get; set; }

        /// <summary>
        /// Gb321處理狀況(最新)
        /// </summary>
        public string Gb321ProType { get; set; }

        /// <summary>
        /// Gb321執行時間
        /// </summary>
        public DateTime? UpdateGb321Time { get; set; }

        /// <summary>
        /// Gb353查詢狀態
        /// </summary>
        public string Gb353Status { get; set; }

        /// <summary>
        /// Gb353訊息
        /// </summary>
        public string Gb353Msg { get; set; }

        /// <summary>
        /// GB353錯單時間
        /// </summary>
        public string Gb353IssueDateTime { get; set; }

        /// <summary>
        /// 錯單原因代碼
        /// </summary>
        public string Gb353RejReasonCode { get; set; }

        /// <summary>
        /// 錯單時間+錯單代碼
        /// </summary>
        public string Gb353RejReason { get; set; }

        /// <summary>
        /// 錯單原因說明
        /// </summary>
        public string Gb353RejReasonDesc { get; set; }

        public DateTime? UpdateGb353Time { get; set; }
    }
}
