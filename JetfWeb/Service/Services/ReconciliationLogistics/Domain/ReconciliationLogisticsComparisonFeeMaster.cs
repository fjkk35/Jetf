using System;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳比對明細使用的費用主檔資料。
    /// </summary>
    public sealed class ReconciliationLogisticsComparisonFeeMaster
    {
        /// <summary>費用主檔識別碼。</summary>
        public int Id { get; set; }

        /// <summary>資料來源。</summary>
        public string Source { get; set; }

        /// <summary>報關類型。</summary>
        public string Type { get; set; }

        /// <summary>客戶名稱。</summary>
        public string Customer { get; set; }

        /// <summary>清關袋號。</summary>
        public string BagNumber { get; set; }

        /// <summary>分提單號。</summary>
        public string TrackingNo { get; set; }

        /// <summary>物流貨號。</summary>
        public string DlvInv { get; set; }

        /// <summary>新物流單號。</summary>
        public string OutboundTrackingNo { get; set; }

        /// <summary>出倉時間。</summary>
        public DateTime? OutDateTime { get; set; }

        /// <summary>捷豐應收總計。</summary>
        public string ToDlvCod { get; set; }

        /// <summary>跟派件收金額。</summary>
        public int? TransCod { get; set; }

        /// <summary>報關費。</summary>
        public int? Ccfee { get; set; }

        /// <summary>到付款金額。</summary>
        public int? Cod { get; set; }

        /// <summary>手續費。</summary>
        public int? Fee { get; set; }

        /// <summary>原納稅義務人。</summary>
        public string Recipient { get; set; }

        /// <summary>納稅義務人。</summary>
        public string TaxPayer { get; set; }

        /// <summary>納稅義務人身分證號。</summary>
        public string TaxRecId { get; set; }

    }
}
