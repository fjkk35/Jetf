namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞託運資料查詢列資料。
    /// </summary>
    public class SeaShenzhenOriginalQueryRow
    {
        /// <summary>
        /// 主鍵。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        public string DataDateText { get; set; }

        /// <summary>
        /// 報關行。
        /// </summary>
        public string DataTypeDisplay { get; set; }

        /// <summary>
        /// 報關號碼。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 提單號碼。
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 訂單編號。
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// 託運單號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 託運單號是否有簽收單。
        /// </summary>
        public bool HasJetfSerialSignReceipt { get; set; }

        /// <summary>
        /// 廠商交易時間。
        /// </summary>
        public string TransTimeText { get; set; }

        /// <summary>
        /// 寄件通路。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 收件人姓名。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 收件門市地址。
        /// </summary>
        public string ImporterAddress { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 商品名稱。
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// 代收金額。
        /// </summary>
        public string CcText { get; set; }

        /// <summary>
        /// 稅金。
        /// </summary>
        public int? Tax { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 數量。
        /// </summary>
        public string QuantityText { get; set; }

        /// <summary>
        /// 材積。
        /// </summary>
        public decimal? Volume { get; set; }

        /// <summary>
        /// 重量。
        /// </summary>
        public string GwText { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 認領人。
        /// </summary>
        public string Claimant { get; set; }

        /// <summary>
        /// 稅金支付方式。
        /// </summary>
        public string TaxPayment { get; set; }
    }
}
