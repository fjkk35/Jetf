using System.Collections.Generic;

namespace Service.Services.Ezway.Domain
{
    /// <summary>
    /// Ezway 查詢回應內容。
    /// </summary>
    public class EzwayQueryResponse
    {
        /// <summary>
        /// 查詢結果清單。
        /// </summary>
        public List<EzwayQueryResult> Results { get; set; } = new List<EzwayQueryResult>();

        /// <summary>
        /// 查詢後更新的驗證碼狀態。
        /// </summary>
        public EzwayCaptchaState QueryCaptchaState { get; set; } = new EzwayCaptchaState();
    }
}