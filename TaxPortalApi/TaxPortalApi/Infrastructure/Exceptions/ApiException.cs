namespace TaxPortalApi.Infrastructure.Exceptions;

/// <summary>
/// 包含 HTTP 狀態碼與回應訊息的 API 例外。
/// </summary>
/// <param name="statusCode">回應的 HTTP 狀態碼。</param>
/// <param name="message">提供給呼叫端的錯誤訊息。</param>
/// <param name="errorCode">選填的錯誤代碼，未指定時為空字串。</param>
public sealed class ApiException(int statusCode, string message, string errorCode = "") : Exception(message)
{
    /// <summary>
    /// 回應的 HTTP 狀態碼。
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// 錯誤代碼，未指定時為空字串。
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}
