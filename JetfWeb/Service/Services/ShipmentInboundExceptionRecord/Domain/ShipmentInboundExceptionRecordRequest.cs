using System.Collections.Generic;

namespace Service.Services.ShipmentInboundExceptionRecord.Domain
{
    /// <summary>
    /// 異常紀錄查詢條件。
    /// </summary>
    public class ShipmentInboundExceptionRecordRequest
    {
        /// <summary>
        /// 入庫日期起日，格式 yyyy-MM-dd。
        /// </summary>
        public string InboundDateStart { get; set; }

        /// <summary>
        /// 入庫日期迄日，格式 yyyy-MM-dd。
        /// </summary>
        public string InboundDateEnd { get; set; }

        /// <summary>
        /// 主號查詢條件。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 單號查詢條件。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 單一客戶代號查詢條件。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 多選客戶代號清單。
        /// </summary>
        public List<string> CustCodes { get; set; }

        /// <summary>
        /// 多選異常原因清單；空清單代表全部。
        /// </summary>
        public List<string> ExceptionReasons { get; set; }

        /// <summary>
        /// 頁碼。
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }
    }
}
