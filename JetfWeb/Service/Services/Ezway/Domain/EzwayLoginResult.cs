namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 登入結果。
    /// </summary>
    public class EzwayLoginResult
    {
        /// <summary>
        /// 是否已成功登入。
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// 是否需要先同意服務條款。
        /// </summary>
        public bool RequiresTermsAgreement { get; set; }

        /// <summary>
        /// 待顯示的服務條款 HTML。
        /// </summary>
        public string TermsHtml { get; set; }

        /// <summary>
        /// 本次登入成功的帳號資訊。
        /// </summary>
        public EzwayLoggedInAccount CurrentAccount { get; set; }
    }
}