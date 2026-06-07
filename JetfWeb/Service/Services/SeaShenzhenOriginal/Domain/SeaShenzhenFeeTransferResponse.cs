using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 深圳稅金轉檔結果。
    /// </summary>
    public class SeaShenzhenFeeTransferResponse
    {
        /// <summary>
        /// 本次轉檔的資料日期。
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 來源 FeeMaster 筆數。
        /// </summary>
        public int SourceCount { get; set; }

        /// <summary>
        /// 重轉時刪除的舊資料筆數。
        /// </summary>
        public int DeletedCount { get; set; }

        /// <summary>
        /// 本次成功建立的轉檔資料筆數。
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// 異常筆數。
        /// </summary>
        public int ExceptionCount { get; set; }

        /// <summary>
        /// 異常明細。
        /// </summary>
        public List<SeaShenzhenFeeTransferExceptionRow> Exceptions { get; set; } = new List<SeaShenzhenFeeTransferExceptionRow>();
    }

    /// <summary>
    /// 深圳稅金轉檔異常列。
    /// </summary>
    public class SeaShenzhenFeeTransferExceptionRow
    {
        /// <summary>
        /// 異常原因。
        /// </summary>
        public string Reason { get; set; }

        public string MainNumber { get; set; }

        public string TrackingNo { get; set; }

        public string DlvInv { get; set; }

        public string Recipient { get; set; }

        public string RecPhone { get; set; }

        public string RecAddress { get; set; }

        public int Tax1 { get; set; }

        public int Tax2 { get; set; }
    }
}