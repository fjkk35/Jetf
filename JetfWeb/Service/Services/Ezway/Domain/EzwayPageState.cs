using System.Collections.Generic;

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

        /// <summary>
        /// 目前已選取的登入帳號資訊。
        /// </summary>
        public EzwayLoggedInAccount CurrentAccount { get; set; }

        /// <summary>
        /// 目前 session 中可用的已登入帳號清單。
        /// </summary>
        public List<EzwayLoggedInAccount> LoggedInAccounts { get; set; } = new List<EzwayLoggedInAccount>();
    }

    /// <summary>
    /// Ezway 已登入帳號資訊。
    /// </summary>
    public class EzwayLoggedInAccount
    {
        /// <summary>
        /// 目前帳號對應的 session key。
        /// </summary>
        public string AccountSessionKey { get; set; }

        /// <summary>
        /// 登入別代碼。
        /// </summary>
        public string LoginProfileKey { get; set; }

        /// <summary>
        /// 登入別顯示名稱。
        /// </summary>
        public string LoginProfileLabel { get; set; }

        /// <summary>
        /// 統一編號。
        /// </summary>
        public string CompanyId { get; set; }

        /// <summary>
        /// Ezway 帳號。
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 是否可使用 X4 查詢。
        /// </summary>
        public bool CanUseX4 { get; set; }
    }

    /// <summary>
    /// 切換已登入 Ezway 帳號請求。
    /// </summary>
    public class EzwayActivateAccountRequest
    {
        /// <summary>
        /// 目標帳號的 session key。
        /// </summary>
        public string AccountSessionKey { get; set; }
    }
}