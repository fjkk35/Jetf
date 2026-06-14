using System.Collections.Generic;

namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞深圳稅金轉檔異常明細匯出請求。
    /// </summary>
    public class SeaShenzhenTaxTransferExceptionExportRequest
    {
        /// <summary>
        /// 需要匯出的轉檔異常明細。
        /// </summary>
        public List<SeaShenzhenTaxTransferExceptionRow> Exceptions { get; set; } = new List<SeaShenzhenTaxTransferExceptionRow>();
    }
}
