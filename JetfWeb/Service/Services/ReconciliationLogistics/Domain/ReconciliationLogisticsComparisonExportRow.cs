using System;
using System.Collections.Generic;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳比對明細 Excel 匯出資料列。
    /// </summary>
    public sealed class ReconciliationLogisticsComparisonExportRow
    {
        /// <summary>回款日期。</summary>
        public DateTime? RepaymentDate { get; set; }

        /// <summary>出倉時間。</summary>
        public DateTime? OutDateTime { get; set; }

        /// <summary>報關類別。</summary>
        public string Type { get; set; }

        /// <summary>客戶代號。</summary>
        public string Customer { get; set; }

        /// <summary>清關袋號。</summary>
        public string BagNumber { get; set; }

        /// <summary>分提單號。</summary>
        public string TrackingNo { get; set; }

        /// <summary>物流貨號。</summary>
        public string DlvInv { get; set; }

        /// <summary>物流回款金額。</summary>
        public int? ReceivedAmount { get; set; }

        /// <summary>捷豐應收總計。</summary>
        public int? ToDlvCod { get; set; }

        /// <summary>差異金額。</summary>
        public int? DifferenceAmount { get; set; }

        /// <summary>跟派件收。</summary>
        public int? TransCod { get; set; }

        /// <summary>報關費。</summary>
        public int? Ccfee { get; set; }

        /// <summary>到付款。</summary>
        public int? Cod { get; set; }

        /// <summary>手續費。</summary>
        public int? Fee { get; set; }

        /// <summary>納稅義務人。</summary>
        public string TaxPayer { get; set; }

        /// <summary>納稅義務人身分證號。</summary>
        public string TaxRecId { get; set; }

        /// <summary>資料來源（倉別）。</summary>
        public string Source { get; set; }

        /// <summary>上傳檔案原始欄位值。</summary>
        public List<string> UploadedValues { get; set; } = new List<string>();
    }
}
