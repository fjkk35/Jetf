using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 深圳稅金轉檔結果。
    /// </summary>
    public class SeaShenzhenFeeTransferResponse
    {
        public string DataDate { get; set; }

        public int SourceCount { get; set; }

        public int DeletedCount { get; set; }

        public int CreatedCount { get; set; }

        public int ExceptionCount { get; set; }

        public List<SeaShenzhenFeeTransferExceptionRow> Exceptions { get; set; } = new List<SeaShenzhenFeeTransferExceptionRow>();
    }

    /// <summary>
    /// 深圳稅金轉檔異常列。
    /// </summary>
    public class SeaShenzhenFeeTransferExceptionRow
    {
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