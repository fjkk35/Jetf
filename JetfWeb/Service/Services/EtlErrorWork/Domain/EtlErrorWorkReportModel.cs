using System;

namespace Service.Services.EtlErrorWork.Domain
{
    /// <summary>
    /// 空快錯單統計報表資料模型
    /// </summary>
    public class EtlErrorWorkReportModel
    {
        /// <summary>
        /// 客戶
        /// </summary>
        public string CUST { get; set; }

        /// <summary>
        /// 問題原因
        /// </summary>
        public string REASON { get; set; }

        /// <summary>
        /// 主提單號
        /// </summary>
        public string MAWB { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string HAWB { get; set; }

        /// <summary>
        /// 袋號
        /// </summary>
        public string BAG_NO { get; set; }

        /// <summary>
        /// 發出時間
        /// </summary>
        public DateTime? OUT_TIME { get; set; }

        /// <summary>
        /// 發行日期
        /// </summary>
        public DateTime? ISSUEDATE { get; set; }

        /// <summary>
        /// 資料日期 (格式化後)
        /// </summary>
        public string DATADATE { get; set; }
    }
}
