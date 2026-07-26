namespace Service.Services.ReceivableCod.Domain
{
    /// <summary>
    /// 到付款應收未收明細畫面資料。
    /// </summary>
    public sealed class ReceivableCodListItem
    {
        /// <summary>
        /// 資料識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 掛帳日期。
        /// </summary>
        public string PostingDate { get; set; }

        /// <summary>
        /// 資料來源顯示名稱。
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
        /// 客戶中文名稱；查無名稱時顯示客戶代號。
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
        /// 到付款應收金額。
        /// </summary>
        public decimal ReceivableAmount { get; set; }

        /// <summary>
        /// 已收金額。
        /// </summary>
        public decimal ReceivedAmount { get; set; }

        /// <summary>
        /// 尚未收回金額。
        /// </summary>
        public decimal UnreceivedAmount { get; set; }
    }
}
