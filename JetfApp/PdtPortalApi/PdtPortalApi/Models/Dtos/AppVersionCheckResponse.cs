namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// App 版本檢查結果。
/// </summary>
public sealed class AppVersionCheckResponse
{
    /// <summary>
    /// 後端目前允許的最新版本號。
    /// </summary>
    public string LatestVersionCode { get; set; } = string.Empty;

    /// <summary>
    /// APK 下載位置。
    /// </summary>
    public string ApkUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要強制更新。
    /// </summary>
    public bool ForceUpdate { get; set; }

    /// <summary>
    /// 提示訊息。
    /// </summary>
    public string Message { get; set; } = string.Empty;
}