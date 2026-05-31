namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 登入請求模型。
    /// </summary>
    public class EzwayLoginRequest
    {
        /// <summary>
        /// 業者統一編號。
        /// </summary>
        public string CompanyId { get; set; }

        /// <summary>
        /// Ezway 登入帳號。
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Ezway 登入密碼。
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 操作人員輸入的登入驗證碼。
        /// </summary>
        public string Captcha { get; set; }

        /// <summary>
        /// 登入驗證碼識別碼。
        /// </summary>
        public string CaptchaCode { get; set; }

        /// <summary>
        /// 是否需要輸入登入驗證碼。
        /// </summary>
        public bool CaptchaRequired { get; set; }

        /// <summary>
        /// 是否已同意 Ezway 服務條款。
        /// </summary>
        public bool TermsAccepted { get; set; }
    }
}