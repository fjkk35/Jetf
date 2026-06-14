using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞深圳稅金轉檔異常匯出條件。
    /// </summary>
    public class SeaShenzhenFeeTransferExceptionExportRequest
    {
        /// <summary>
        /// 匯出時使用的資料日期。
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 目前畫面上的轉檔異常明細。
        /// </summary>
        public List<SeaShenzhenFeeTransferExceptionRow> Exceptions { get; set; } = new List<SeaShenzhenFeeTransferExceptionRow>();
    }
}