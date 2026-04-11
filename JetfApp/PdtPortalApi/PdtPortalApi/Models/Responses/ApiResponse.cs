using Serilog;

namespace PdtPortalApi.Models.Responses;

/// <summary>
/// API 回應模型（無資料）。
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 狀態碼。
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 訊息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 錯誤碼。
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// 時間戳記。
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.Now;

    /// <summary>
    /// 成功回應。
    /// </summary>
    public static ApiResponse Ok(string message = "操作成功")
    {
        try
        {
            return new ApiResponse
            {
                IsSuccess = true,
                Code = StatusCodes.Status200OK,
                Message = message
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ApiResponse 成功回應失敗");
            throw;
        }
    }

    /// <summary>
    /// 失敗回應。
    /// </summary>
    public static ApiResponse Fail(string message, int code = StatusCodes.Status400BadRequest)
    {
        try
        {
            return new ApiResponse
            {
                IsSuccess = false,
                Code = code,
                Message = message
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ApiResponse 失敗回應失敗");
            throw;
        }
    }

    /// <summary>
    /// 失敗回應（含錯誤代碼）。
    /// </summary>
    public static ApiResponse Fail(string errorCode, string message, int code = StatusCodes.Status400BadRequest)
    {
        try
        {
            return new ApiResponse
            {
                IsSuccess = false,
                Code = code,
                Message = message,
                ErrorCode = errorCode
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ApiResponse 錯誤碼回應失敗");
            throw;
        }
    }
}

/// <summary>
/// API 回應模型。
/// </summary>
/// <typeparam name="T">資料型別。</typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// 資料。
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 成功回應。
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "操作成功")
    {
        try
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Code = StatusCodes.Status200OK,
                Message = message,
                Data = data
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ApiResponse<T> 成功回應失敗");
            throw;
        }
    }

    /// <summary>
    /// 失敗回應。
    /// </summary>
    public new static ApiResponse<T> Fail(string message, int code = StatusCodes.Status400BadRequest)
    {
        try
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Code = code,
                Message = message
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ApiResponse<T> 失敗回應失敗");
            throw;
        }
    }

    /// <summary>
    /// 失敗回應（含錯誤代碼）。
    /// </summary>
    public new static ApiResponse<T> Fail(string errorCode, string message, int code = StatusCodes.Status400BadRequest)
    {
        try
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Code = code,
                Message = message,
                ErrorCode = errorCode
            };
        }
        catch (Exception exception)
        {
            Log.Error(exception, "建立 ApiResponse<T> 錯誤碼回應失敗");
            throw;
        }
    }
}