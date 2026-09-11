namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細修改請求。
    /// </summary>
    public sealed class ReceivableEditRequest
    {
        /// <summary>
        /// 費用主檔明細識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 跟廠商收金額。
        /// </summary>
        public int? CustomerCod { get; set; }

        /// <summary>
        /// 跟派件收金額。
        /// </summary>
        public int? TransCod { get; set; }

        /// <summary>
        /// 捷豐支付金額。
        /// </summary>
        public int? JetfPayment { get; set; }

        /// <summary>
        /// 報關費。
        /// </summary>
        public int? Ccfee { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public int? Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int? Fee { get; set; }

        /// <summary>
        /// 未回收原因。
        /// </summary>
        public string UnreceivedReason { get; set; }
    }
}
