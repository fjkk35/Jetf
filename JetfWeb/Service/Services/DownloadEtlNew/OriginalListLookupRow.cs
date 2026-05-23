namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示原始單資料查詢後的中繼欄位。
    /// </summary>
    internal sealed class OriginalListLookupRow
    {
        /// <summary>
        /// 取得或設定原始單識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 取得或設定主提單號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 取得或設定袋號。
        /// </summary>
        public string BagNo { get; set; }

        /// <summary>
        /// 取得或設定追蹤單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 取得或設定收件人。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 取得或設定收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 取得或設定收件地址。
        /// </summary>
        public string RecAddress { get; set; }

        /// <summary>
        /// 取得或設定收件人證號。
        /// </summary>
        public string RecId { get; set; }

        /// <summary>
        /// 取得或設定代收金額字串。
        /// </summary>
        public string Cc { get; set; }

        /// <summary>
        /// 取得或設定客戶代碼。
        /// </summary>
        public string DespatchNo { get; set; }

        /// <summary>
        /// 取得或設定 TrackingUb 欄位。
        /// </summary>
        public string TrackingUb { get; set; }

        /// <summary>
        /// 取得或設定物流貨號。
        /// </summary>
        public string DeliveryNo { get; set; }

        /// <summary>
        /// 取得或設定稅金支付物流。
        /// </summary>
        public string TransTaxPayment { get; set; }

        /// <summary>
        /// 取得或設定到站資料。
        /// </summary>
        public string Ecm { get; set; }
    }
}