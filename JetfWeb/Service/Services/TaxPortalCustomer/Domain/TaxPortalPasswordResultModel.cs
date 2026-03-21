namespace Service.Services.TaxPortalCustomerService.Domain
{
    /// <summary>
    /// 帳號密碼顯示結果。
    /// </summary>
    public class TaxPortalPasswordResultModel
    {
        /// <summary>
        /// 帳號。
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 明文密碼。
        /// </summary>
        public string Password { get; set; }
    }
}