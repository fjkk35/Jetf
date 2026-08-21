using System;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳比對明細使用的到付款資料。
    /// </summary>
    public sealed class ReconciliationLogisticsFeeMasterCodMatch
    {
        /// <summary>到付款資料識別碼。</summary>
        public int Id { get; set; }

        /// <summary>資料來源類型。</summary>
        public string DataType { get; set; }

        /// <summary>客戶代號或名稱。</summary>
        public string Customer { get; set; }

        /// <summary>清關袋號。</summary>
        public string BagNumber { get; set; }

        /// <summary>分提單號。</summary>
        public string TrackingNo { get; set; }

        /// <summary>物流貨號。</summary>
        public string DlvInv { get; set; }

        /// <summary>到付款金額。</summary>
        public decimal Cc { get; set; }

        /// <summary>運費。</summary>
        public int? FreightFee { get; set; }

        /// <summary>手續費。</summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 應向物流代收金額。
        /// </summary>
        public int? ToDlvCod { get; set; }

        /// <summary>出倉時間。</summary>
        public DateTime SignOutTime { get; set; }

        /// <summary>對應的物流銷帳紀錄識別碼。</summary>
        public int? ReconciliationLogisticsId { get; set; }
    }
}
