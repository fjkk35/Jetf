namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 查詢條件請求模型。
    /// </summary>
    public class EzwayQueryRequest
    {
        /// <summary>
        /// 查詢方式，單筆查詢固定為 Y，整批查詢固定為 N。
        /// </summary>
        public string Manual { get; set; } = "Y";

        /// <summary>
        /// 查詢頁類型。
        /// X4: 預先委任確認查詢(X4)
        /// Simple: 預先委任確認查詢(簡易)
        /// </summary>
        public string QueryApiType { get; set; } = "Simple";

        /// <summary>
        /// 預報關日期起。
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// 預報關日期迄。
        /// </summary>
        public string EndDate { get; set; }

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
        /// 委任狀態。
        /// </summary>
        public string Status { get; set; } = "A";

        /// <summary>
        /// 海關回覆狀態。
        /// </summary>
        public string AuthorizeStatus { get; set; } = "A";

        /// <summary>
        /// 操作人員輸入的查詢驗證碼。
        /// </summary>
        public string QueryCaptcha { get; set; }

        /// <summary>
        /// 查詢驗證碼 API 回傳的驗證碼識別碼。
        /// </summary>
        public string QueryCaptchaCode { get; set; }

        /// <summary>
        /// 是否需要輸入查詢驗證碼。
        /// </summary>
        public bool QueryCaptchaRequired { get; set; }
    }
}