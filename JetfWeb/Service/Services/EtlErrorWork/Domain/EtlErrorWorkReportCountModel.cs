using System;

namespace Service.Services.EtlErrorWork.Domain
{
    /// <summary>
    /// 空快錯單統計報表傳輸筆數模型
    /// </summary>
    public class EtlErrorWorkReportCountModel
    {
        /// <summary>
        /// 客戶
        /// </summary>
        public string CUST { get; set; }

        /// <summary>
        /// 發行日期
        /// </summary>
        public string ISSUEDATE { get; set; }

        /// <summary>
        /// 總計
        /// </summary>
        public int TOTAL { get; set; }

        /// <summary>
        /// 資料日期 (格式化後)
        /// </summary>
        public string DATADATE { get; set; }
    }
}
