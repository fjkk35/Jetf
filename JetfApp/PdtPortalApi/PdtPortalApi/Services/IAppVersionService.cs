using PdtPortalApi.Models.Dtos;

namespace PdtPortalApi.Services;

/// <summary>
/// App 版本檢查服務介面。
/// </summary>
public interface IAppVersionService
{
    /// <summary>
    /// 取得版本檢查結果。
    /// </summary>
    /// <param name="versionCode">客戶端版本號。</param>
    /// <returns>版本檢查結果。</returns>
    AppVersionCheckResponse GetVersionCheckResult(string versionCode);

    /// <summary>
    /// 驗證客戶端版本是否允許登入。
    /// </summary>
    /// <param name="versionCode">客戶端版本號。</param>
    /// <returns>允許時回傳 true，否則回傳 false。</returns>
    bool IsVersionAllowed(string versionCode);
}