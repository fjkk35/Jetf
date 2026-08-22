namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳上傳檔案單筆資料。
    /// </summary>
    public sealed class ReconciliationLogisticsUploadRow
    {
        /// <summary>
        /// 上傳檔案列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 分提單號。
        /// <para>新竹物流清單格式：清單編號。</para>
        /// <para>新竹物流匯款明細格式：出貨單號。</para>
        /// <para>7-11：訂單號碼。</para>
        /// <para>客樂得：訂單號碼。</para>
        /// <para>大榮：出貨單號。</para>
        /// <para>超峰：订单号。</para>
        /// <para>圓通：原單號。</para>
        /// <para>關貿：分提單號碼。</para>
        /// <para>全家：不使用此欄位。</para>
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// <para>新竹物流清單格式：查貨號碼。</para>
        /// <para>新竹物流匯款明細格式：宅配單號。</para>
        /// <para>7-11：出貨單號。</para>
        /// <para>客樂得及超峰：託運單號。</para>
        /// <para>大榮：移除「空白＋00」後綴的明細單號。</para>
        /// <para>現金：運單號。</para>
        /// <para>圓通：圆通单号。</para>
        /// <para>關貿：不使用此欄位。</para>
        /// <para>全家：廠商訂單編號。</para>
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 回收金額原始文字。
        /// </summary>
        public string ReceivedAmountText { get; set; }

        /// <summary>
        /// 回收金額。
        /// <para>客樂得及大榮：實收金額。</para>
        /// <para>超峰：應收金額。</para>
        /// <para>現金：金額。</para>
        /// <para>圓通：合计。</para>
        /// <para>關貿：交易金額。</para>
        /// <para>全家：交易狀態為 1 且大於 0 的代收金額。</para>
        /// </summary>
        public int? ReceivedAmount { get; set; }

        /// <summary>
        /// 新竹物流客戶代號或客戶別。
        /// </summary>
        public string CustomerCode { get; set; }

        /// <summary>
        /// 7-11 備註。
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// 驗證失敗原因。
        /// </summary>
        public string FailReason { get; set; }
    }
}
