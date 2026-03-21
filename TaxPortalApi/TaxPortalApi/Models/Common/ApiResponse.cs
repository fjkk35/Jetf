using Microsoft.AspNetCore.Http;

namespace TaxPortalApi.Models.Common;

/// <summary>
/// API 回應模型
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// 資料
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 成功回應
    /// </summary>
    /// <param name="data"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static ApiResponse<T> Ok(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public new static ApiResponse<T> Fail(string message, int code = 400)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Code = code,
            Message = message
        };
    }

    public new static ApiResponse<T> Fail(string errorCode, string message, int code = 400)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Code = code,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// API 回應模型（無資料）
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 狀態碼
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 錯誤碼
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 時間戳記 - 使用目前系統的 UTC + 8 時間
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.Now;

    /// <summary>
    /// 成功回應
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static ApiResponse Ok(string message = "操作成功")
    {
        return new ApiResponse
        {
            IsSuccess = true,
            Code = 200,
            Message = message
        };
    }

    /// <summary>
    /// 失敗回應
    /// </summary>
    /// <param name="message"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public static ApiResponse Fail(string message, int code = 400)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            Code = code,
            Message = message
        };
    }

    /// <summary>
    /// 失敗回應（含錯誤代碼）
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="message"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public static ApiResponse Fail(string errorCode, string message, int code = 400)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            Code = code,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
