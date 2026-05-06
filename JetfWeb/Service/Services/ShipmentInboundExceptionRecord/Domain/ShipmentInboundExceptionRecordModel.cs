using System;
using System.Collections.Generic;

namespace Service.Services.ShipmentInboundExceptionRecord.Domain
{
    /// <summary>
    /// 貨件回倉異常紀錄查詢結果列。
    /// </summary>
    public class ShipmentInboundExceptionRecordModel
    {
        /// <summary>
        /// 貨件入庫資料 Id。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 入庫日期。
        /// </summary>
        public DateTime InboundDate { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 最新異常原因 Id。
        /// </summary>
        public int? ExceptionReasonId { get; set; }

        /// <summary>
        /// 最新異常原因。
        /// </summary>
        public string ExceptionReason { get; set; }
    }

    /// <summary>
    /// 匯出 ZIP 時使用的異常圖片資料。
    /// </summary>
    public class ShipmentInboundExceptionImageExportModel
    {
        /// <summary>
        /// 貨件入庫資料 Id。
        /// </summary>
        public int ShipmentInboundId { get; set; }

        /// <summary>
        /// 異常原因 Id。
        /// </summary>
        public int? ExceptionReasonId { get; set; }

        /// <summary>
        /// 圖片實體或相對路徑。
        /// </summary>
        public string FilePath { get; set; }
    }
}
