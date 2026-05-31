namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 查詢結果欄位。
    /// </summary>
    public class EzwayQueryResult
    {
        /// <summary>
        /// Ezway 查詢結果識別碼。
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 交易識別碼。
        /// </summary>
        public string TransactionId { get; set; }

        /// <summary>
        /// 報單類型。
        /// </summary>
        public string DeclType { get; set; }

        /// <summary>
        /// 報關業者名稱。
        /// </summary>
        public string BrokerName { get; set; }

        /// <summary>
        /// 驗證類型。
        /// </summary>
        public string VerifiedType { get; set; }

        /// <summary>
        /// 預報關日期。
        /// </summary>
        public string ImportDate { get; set; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        public string DeclNo { get; set; }

        /// <summary>
        /// 主提單號碼。
        /// </summary>
        public string MawbNo { get; set; }

        /// <summary>
        /// 分提單號碼。
        /// </summary>
        public string HawbNo { get; set; }

        /// <summary>
        /// 實名委任日期。
        /// </summary>
        public string ReplyDate { get; set; }

        /// <summary>
        /// 實名委任時間。
        /// </summary>
        public string ReplyTime { get; set; }

        /// <summary>
        /// 認證結果。
        /// </summary>
        public string IsReply { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 核准文號。
        /// </summary>
        public string AuthorizeDocNo { get; set; }

        /// <summary>
        /// 海關回覆日期。
        /// </summary>
        public string AuthorizeDatm { get; set; }

        /// <summary>
        /// 海關回覆結果。
        /// </summary>
        public string AuthorizeReply { get; set; }

        /// <summary>
        /// 證件號碼。
        /// </summary>
        public string IdNo { get; set; }

        /// <summary>
        /// 電話號碼。
        /// </summary>
        public string TelNo { get; set; }

        /// <summary>
        /// 阻擋原因。
        /// </summary>
        public string BlockReason { get; set; }
    }
}