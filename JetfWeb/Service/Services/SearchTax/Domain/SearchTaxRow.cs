namespace Service.Services.SearchTax.Domain
{
    /// <summary>
    /// 稅金查詢結果資料列（費用主檔）。
    /// </summary>
    public sealed class SearchTaxRow
    {
        /// <summary>
        /// 費用主檔主鍵。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 作業日。
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 倉儲類型。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 報關類型。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 主提單號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 清關袋號（海運為分提單號、空運為袋號）。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 收件人名稱。
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string RecPhone { get; set; }

        /// <summary>
        /// 收件人地址。
        /// </summary>
        public string RecAddress { get; set; }

        /// <summary>
        /// 進倉時間（yyyy-MM-dd HH:mm:ss）。
        /// </summary>
        public string InDateTimeText { get; set; }

        /// <summary>
        /// 出倉日期（yyyy-MM-dd）。
        /// </summary>
        public string OutDateText { get; set; }

        /// <summary>
        /// 出倉時間（yyyy-MM-dd HH:mm:ss）。
        /// </summary>
        public string OutDateTimeText { get; set; }

        /// <summary>
        /// 稅金類別名稱。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 稅金1。
        /// </summary>
        public int Tax1 { get; set; }

        /// <summary>
        /// 稅金2。
        /// </summary>
        public int Tax2 { get; set; }

        /// <summary>
        /// 稅金合計。
        /// </summary>
        public int TotalTax { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int? CcFee { get; set; }

        /// <summary>
        /// 代收手續費。
        /// </summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        public int? Cod { get; set; }

        /// <summary>
        /// 物流代收款。
        /// </summary>
        public string ToDlvCod { get; set; }
    }
}
