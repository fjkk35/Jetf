namespace PdtPortalApi.Options;

/// <summary>
/// App 版本設定。
/// </summary>
public sealed class AppVersionOptions
{
    /// <summary>
    /// 設定區段名稱。
    /// </summary>
    public const string SectionName = "AppVersion";

    /// <summary>
    /// 目前允許登入的最新版本號。
    /// </summary>
    public string LatestVersionCode { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要強制更新。
    /// </summary>
    public bool ForceUpdate { get; set; }

    /// <summary>
    /// APK 下載網址。
    /// </summary>
    public string ApkUrl { get; set; } = string.Empty;

    /// <summary>
    /// APK 實體檔案路徑。
    /// </summary>
    public string ApkFilePath { get; set; } = string.Empty;

    /// <summary>
    /// APK 下載檔名。
    /// </summary>
    public string ApkFileName { get; set; } = "JETFApp-release.apk";
}