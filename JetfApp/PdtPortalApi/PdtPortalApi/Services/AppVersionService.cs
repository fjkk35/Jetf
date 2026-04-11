using Microsoft.Extensions.Options;
using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Options;

namespace PdtPortalApi.Services;

/// <summary>
/// App 版本檢查服務。
/// </summary>
public sealed class AppVersionService(IOptions<AppVersionOptions> options, ILogger<AppVersionService> logger) : IAppVersionService
{
    private readonly AppVersionOptions _options = options.Value;
    private readonly ILogger<AppVersionService> _logger = logger;

    /// <summary>
    /// 取得版本檢查結果。
    /// </summary>
    /// <param name="versionCode">客戶端版本號。</param>
    /// <returns>版本檢查結果。</returns>
    public AppVersionCheckResponse GetVersionCheckResult(string versionCode)
    {
        try
        {
            var isLatestVersion = string.Equals(versionCode, _options.LatestVersionCode, StringComparison.OrdinalIgnoreCase);
            var forceUpdate = !isLatestVersion && _options.ForceUpdate;
            var message = isLatestVersion
                ? "版本正確，可正常使用"
                : forceUpdate
                    ? "目前版本過舊，請更新後再使用"
                    : "目前有新版本，可選擇更新或繼續使用";

            return new AppVersionCheckResponse
            {
                LatestVersionCode = _options.LatestVersionCode,
                ApkUrl = _options.ApkUrl,
                ForceUpdate = forceUpdate,
                Message = message
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "產生 App 版本檢查結果失敗，VersionCode: {VersionCode}", versionCode);
            throw;
        }
    }

    /// <summary>
    /// 驗證客戶端版本是否允許登入。
    /// </summary>
    /// <param name="versionCode">客戶端版本號。</param>
    /// <returns>允許時回傳 true，否則回傳 false。</returns>
    public bool IsVersionAllowed(string versionCode)
    {
        try
        {
            return !_options.ForceUpdate || string.Equals(versionCode, _options.LatestVersionCode, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "驗證 App 版本失敗，VersionCode: {VersionCode}", versionCode);
            return false;
        }
    }
}
