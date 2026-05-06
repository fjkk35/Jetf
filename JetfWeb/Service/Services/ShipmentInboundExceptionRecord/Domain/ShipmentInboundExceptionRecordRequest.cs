using System.Collections.Generic;

namespace Service.Services.ShipmentInboundExceptionRecord.Domain
{
    /// <summary>
    /// 貨件回倉異常紀錄查詢條件。
    /// </summary>
    public class ShipmentInboundExceptionRecordRequest
    {
        /// <summary>
        /// 入庫日期起，格式 yyyy-MM-dd。
        /// </summary>
        public string InboundDateStart { get; set; }

        /// <summary>
        /// 入庫日期迄，格式 yyyy-MM-dd。
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
        /// 客戶代碼查詢條件，保留單一客戶輸入相容性。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 多選客戶代碼清單。
        /// </summary>
        public List<string> CustCodes { get; set; }

        /// <summary>
        /// 異常原因查詢條件。
        /// </summary>
        public string ExceptionReason { get; set; }

        /// <summary>
        /// 目前頁碼。
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }
    }
}
