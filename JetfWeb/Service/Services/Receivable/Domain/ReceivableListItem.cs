namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細畫面資料。
    /// </summary>
    public sealed class ReceivableListItem
    {
        /// <summary>
        /// 費用明細識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 掛帳日。
        /// </summary>
        public string PostingDate { get; set; }

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
        /// 客戶中文名稱。
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        public string OutDateTime { get; set; }

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
        /// 代收小計。
        /// </summary>
        public int CodSubtotal { get; set; }

        /// <summary>
        /// 已收金額。
        /// </summary>
        public int ReceivedAmount { get; set; }

        /// <summary>
        /// 未收金額。
        /// </summary>
        public int UnreceivedAmount { get; set; }

        /// <summary>
        /// 跟廠商收金額。
        /// </summary>
        public int CustomerCod { get; set; }

        /// <summary>
        /// 跟派件收金額。
        /// </summary>
        public int ToDlvCod { get; set; }

        /// <summary>
        /// 捷豐支付，目前保留空白。
        /// </summary>
        public string JetfPayment { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int Ccfee { get; set; }

        /// <summary>
        /// 重派運費，目前保留空白。
        /// </summary>
        public string RedispatchFreight { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int Fee { get; set; }

        /// <summary>
        /// 未回收原因，目前保留空白。
        /// </summary>
        public string UnreceivedReason { get; set; }
    }
}
