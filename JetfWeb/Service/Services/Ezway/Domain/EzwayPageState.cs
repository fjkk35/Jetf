namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 頁面初始化狀態。
    /// </summary>
    public class EzwayPageState
    {
        /// <summary>
        /// 是否已完成登入。
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// 登入階段驗證碼狀態。
        /// </summary>
        public EzwayCaptchaState LoginCaptchaState { get; set; } = new EzwayCaptchaState();

        /// <summary>
        /// 查詢階段驗證碼狀態。
        /// </summary>
        public EzwayCaptchaState QueryCaptchaState { get; set; } = new EzwayCaptchaState();
    }
}