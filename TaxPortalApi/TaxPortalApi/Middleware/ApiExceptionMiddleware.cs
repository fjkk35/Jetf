using TaxPortalApi.Infrastructure.Exceptions;
using TaxPortalApi.Models.Common;

namespace TaxPortalApi.Middleware;

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            await context.Response.WriteAsJsonAsync(ApiResponse<object?>.Fail(
                exception.ErrorCode,
                exception.Message,
                exception.StatusCode));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "發生未處理例外");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(ApiResponse<object?>.Fail(
                "系統發生未預期錯誤",
                StatusCodes.Status500InternalServerError));
        }
    }
}