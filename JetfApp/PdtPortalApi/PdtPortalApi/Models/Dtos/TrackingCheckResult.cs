namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 單號檢查結果。
/// </summary>
public sealed class TrackingCheckResult
{
    /// <summary>
    /// 是否找到原始資料、長度檢查。
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// 回應訊息。
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
