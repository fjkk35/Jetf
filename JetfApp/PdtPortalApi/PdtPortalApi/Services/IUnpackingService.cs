using PdtPortalApi.Models.Dtos;
using PdtPortalApi.Models.Requests;

namespace PdtPortalApi.Services;

/// <summary>
/// 拆袋作業服務（FTZ、TACT）。
/// </summary>
public interface IUnpackingService
{
    /// <summary>
    /// 掃描袋號，判斷是否需要拆袋並回傳尚未掃描的分提單號。
    /// </summary>
    /// <param name="request">袋號掃描請求。</param>
    /// <returns>袋號掃描結果。</returns>
    Task<UnpackingBagResultDto> ScanBagNoAsync(UnpackingBagNoRequest request);

    /// <summary>
    /// 掃描分提單號，符合拆袋資料時更新掃描紀錄。
    /// </summary>
    /// <param name="request">分提單號掃描請求。</param>
    /// <returns>分提單號掃描結果。</returns>
    Task<UnpackingTrackingResultDto> ScanTrackingNoAsync(UnpackingTrackingNoRequest request);

    /// <summary>
    /// 取得袋號尚未掃描的分提單號。
    /// </summary>
    /// <param name="bagNo">袋號。</param>
    /// <returns>尚未掃描的分提單號。</returns>
    Task<IReadOnlyList<string>> GetPendingTrackingNosAsync(string bagNo);
}
