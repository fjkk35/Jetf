namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 驗證碼狀態。
    /// </summary>
    public class EzwayCaptchaState
    {
        /// <summary>
        /// 是否需要顯示並輸入驗證碼。
        /// </summary>
        public bool CaptchaRequired { get; set; }

        /// <summary>
        /// 驗證碼圖片的 Base64 字串。
        /// </summary>
        public string CaptchaImageBase64 { get; set; }

        /// <summary>
        /// 驗證碼識別碼。
        /// </summary>
        public string CaptchaCode { get; set; }
    }
}