using System;

namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細的資料庫投影資料。
    /// </summary>
    internal sealed class ReceivableDataRow
    {
        /// <summary>
        /// 費用明細識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public DateTime? OutDateTime { get; set; }

        /// <summary>
        /// 客戶銷帳時間。
        /// </summary>
        public DateTime? CustomerReconciliationDate { get; set; }

        /// <summary>
        /// 物流銷帳日期。
        /// </summary>
        public DateTime? LogisticsReconciliationDate { get; set; }

        /// <summary>
        /// 資料來源。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 報關類別。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶代號。
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int Ccfee { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 跟廠商收金額。
        /// </summary>
        public int CustomerCod { get; set; }

        /// <summary>
        /// 應向物流公司收取的金額文字。
        /// </summary>
        public string ToDlvCod { get; set; }

        /// <summary>
        /// 已向廠商收回金額。
        /// </summary>
        public int ReceivedCustomerCod { get; set; }

        /// <summary>
        /// 已向物流公司收回應收金額。
        /// </summary>
        public int ReceivedToDlvCod { get; set; }
    }
}
