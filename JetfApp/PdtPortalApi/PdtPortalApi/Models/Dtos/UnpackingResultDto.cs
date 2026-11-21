namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 拆袋作業-袋號掃描結果。
/// </summary>
public sealed class UnpackingBagResultDto
{
    /// <summary>
    /// 是否需要拆袋處理。
    /// </summary>
    public bool NeedUnpacking { get; init; }

    /// <summary>
    /// 尚未掃描的分提單號。
    /// </summary>
    public IReadOnlyList<string> PendingTrackingNos { get; init; } = [];
}

/// <summary>
/// 拆袋作業-分提單號掃描結果。
/// </summary>
public sealed class UnpackingTrackingResultDto
{
    /// <summary>
    /// 是否為需拆出的分提單號。
    /// </summary>
    public bool IsUnpackTarget { get; init; }

    /// <summary>
    /// 拆出提示訊息（備註有值時為備註，否則為「拆出」）。
    /// </summary>
    public string ResultMessage { get; init; } = string.Empty;
}

/// <summary>
/// 拆袋上傳資料列（B6F_UNPACKING_UPLOAD）。
/// </summary>
public sealed class UnpackingUploadRow
{
    /// <summary>
    /// 分提單號。
    /// </summary>
    public string? TrackingNo { get; set; }

    /// <summary>
    /// 備註。
    /// </summary>
    public string? Remark { get; set; }
}
