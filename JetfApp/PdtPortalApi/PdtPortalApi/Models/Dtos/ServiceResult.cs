using Serilog;

namespace PdtPortalApi.Models.Dtos;

/// <summary>
/// 服務處理結果。
/// </summary>
public sealed class ServiceResult
{
    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 結果訊息。
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 錯誤代碼。
    /// </summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>
    /// 狀態碼。
    /// </summary>
    public int Code { get; init; } = StatusCodes.Status200OK;

    /// <summary>
    /// 建立成功結果。
    /// </summary>
    /// <param name="message">成功訊息。</param>
    /// <returns>成功結果。</returns>
    public static ServiceResult Success(string message = "操作成功")
    {
        try
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Message = message,
                Code = StatusCodes.Status200OK
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ServiceResult 成功結果失敗");
            throw;
        }
    }

    /// <summary>
    /// 建立失敗結果。
    /// </summary>
    /// <param name="errorCode">錯誤代碼。</param>
    /// <param name="message">錯誤訊息。</param>
    /// <param name="code">狀態碼。</param>
    /// <returns>失敗結果。</returns>
    public static ServiceResult Fail(string errorCode, string message, int code = StatusCodes.Status400BadRequest)
    {
        try
        {
            return new ServiceResult
            {
                IsSuccess = false,
                ErrorCode = errorCode,
                Message = message,
                Code = code
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ServiceResult 失敗結果失敗");
            throw;
        }
    }
}