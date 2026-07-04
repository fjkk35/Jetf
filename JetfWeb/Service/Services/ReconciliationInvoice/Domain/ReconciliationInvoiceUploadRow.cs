using System;

namespace Service.Services.ReconciliationInvoice.Domain
{
    /// <summary>
    /// 代收銷帳發票上傳的單筆列資料。
    /// </summary>
    public sealed class ReconciliationInvoiceUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 發票類別。
        /// </summary>
        public string InvoiceType { get; set; }

        /// <summary>
        /// 開立日期原始文字。
        /// </summary>
        public string InvoiceDateText { get; set; }

        /// <summary>
        /// 開立日期。
        /// </summary>
        public DateTime? InvoiceDate { get; set; }

        /// <summary>
        /// 發票號碼。
        /// </summary>
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 失敗原因。
        /// </summary>
        public string FailReason { get; set; }
    }
}
