using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PdtPortalApi.Services;

namespace PdtPortalApi.Filters;

public sealed class ApiRequestTraceFilter(
    IApiAuditLogger apiAuditLogger,
    ILogger<ApiRequestTraceFilter> logger) : IAsyncActionFilter, IOrderedFilter
{
    private const string UnknownAccount = "Unknown";
    private const int MaximumCollectionItems = 100;
    private const int MaximumObjectDepth = 5;
    private const int MaximumStringLength = 8_192;

    private static readonly string[] AccountPropertyNames = ["UploadOpe", "EditUser", "Account"];
    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Signature", "X-Signature", "Authorization", "Password", "Secret", "Token", "Photo"
    };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IApiAuditLogger _apiAuditLogger = apiAuditLogger;
    private readonly ILogger<ApiRequestTraceFilter> _logger = logger;

    public int Order => int.MinValue;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var account = ResolveAccount(context);
        var requestPayload = SerializeSafely(BuildRequestPayload(context.ActionArguments));
        ActionExecutedContext? executedContext = null;

        try
        {
            executedContext = await next();
        }
        finally
        {
            stopwatch.Stop();
            try
            {
                var controller = context.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName)
                    ? controllerName ?? string.Empty
                    : string.Empty;
                var action = context.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName)
                    ? actionName ?? string.Empty
                    : string.Empty;
                var statusCode = ResolveStatusCode(context, executedContext);
                var responsePayload = SerializeSafely(BuildResponsePayload(executedContext, statusCode));

                _apiAuditLogger.Log(
                    account,
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path,
                    controller,
                    action,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    requestPayload,
                    responsePayload);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "建立 API 稽核日誌失敗，Path: {Path}", context.HttpContext.Request.Path);
            }
        }
    }

    private static string ResolveAccount(ActionExecutingContext context)
    {
        foreach (var propertyName in AccountPropertyNames)
        {
            var argumentAccount = context.ActionArguments
                .FirstOrDefault(argument => argument.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                .Value as string;
            if (!string.IsNullOrWhiteSpace(argumentAccount))
            {
                return argumentAccount.Trim();
            }

            var account = FindStringProperty(context.ActionArguments.Values, propertyName);
            if (!string.IsNullOrWhiteSpace(account))
            {
                return account.Trim();
            }
        }

        var identityName = context.HttpContext.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(identityName))
        {
            return identityName.Trim();
        }

        var headerAccount = context.HttpContext.Request.Headers["X-Account"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(headerAccount) ? UnknownAccount : headerAccount.Trim();
    }

    private static string? FindStringProperty(IEnumerable<object?> values, string propertyName)
    {
        foreach (var value in values.Where(value => value is not null))
        {
            var property = value!.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property?.PropertyType == typeof(string) && property.GetValue(value) is string propertyValue)
            {
                return propertyValue;
            }
        }

        return null;
    }

    private static object BuildRequestPayload(IDictionary<string, object?> actionArguments)
    {
        return actionArguments
            .Where(argument => argument.Value is not CancellationToken)
            .ToDictionary(
                argument => argument.Key,
                argument => SensitiveNames.Contains(argument.Key)
                    ? "***"
                    : SanitizeValue(argument.Value, argument.Key, 0));
    }

    private static object? BuildResponsePayload(ActionExecutedContext? context, int statusCode)
    {
        if (context is null)
        {
            return new { StatusCode = statusCode, Error = "Action execution failed" };
        }

        if (context.Exception is not null && !context.ExceptionHandled)
        {
            return new
            {
                StatusCode = statusCode,
                Exception = context.Exception.GetType().Name,
                context.Exception.Message
            };
        }

        return context.Result switch
        {
            ObjectResult result => SanitizeValue(result.Value, "Response", 0),
            JsonResult result => SanitizeValue(result.Value, "Response", 0),
            ContentResult result => new
            {
                result.StatusCode,
                result.ContentType,
                Content = Truncate(result.Content)
            },
            FileResult result => new
            {
                Type = result.GetType().Name,
                result.ContentType,
                result.FileDownloadName
            },
            StatusCodeResult result => new { result.StatusCode },
            EmptyResult => null,
            null => new { StatusCode = statusCode },
            _ => new { Type = context.Result.GetType().Name, StatusCode = statusCode }
        };
    }

    private static int ResolveStatusCode(ActionExecutingContext context, ActionExecutedContext? executedContext)
    {
        if (executedContext is null || executedContext.Exception is not null && !executedContext.ExceptionHandled)
        {
            return StatusCodes.Status500InternalServerError;
        }

        return executedContext.Result switch
        {
            ObjectResult result when result.StatusCode.HasValue => result.StatusCode.Value,
            StatusCodeResult result => result.StatusCode,
            _ => context.HttpContext.Response.StatusCode > 0
                ? context.HttpContext.Response.StatusCode
                : StatusCodes.Status200OK
        };
    }

    private static object? SanitizeValue(object? value, string propertyName, int depth)
    {
        if (value is null)
        {
            return null;
        }

        if (SensitiveNames.Contains(propertyName))
        {
            return "***";
        }

        if (depth >= MaximumObjectDepth)
        {
            return Truncate(value.ToString());
        }

        var type = value.GetType();
        if (IsSimpleType(type))
        {
            return value is string text ? Truncate(text) : value;
        }

        if (value is IEnumerable enumerable)
        {
            return enumerable.Cast<object?>()
                .Take(MaximumCollectionItems)
                .Select(item => SanitizeValue(item, "Item", depth + 1))
                .ToList();
        }

        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToDictionary(
                property => property.Name,
                property =>
                {
                    try
                    {
                        return SanitizeValue(property.GetValue(value), property.Name, depth + 1);
                    }
                    catch
                    {
                        return "<unavailable>";
                    }
                });
    }

    private static bool IsSimpleType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        return actualType.IsPrimitive
            || actualType.IsEnum
            || actualType == typeof(string)
            || actualType == typeof(decimal)
            || actualType == typeof(DateTime)
            || actualType == typeof(DateTimeOffset)
            || actualType == typeof(TimeSpan)
            || actualType == typeof(Guid)
            || actualType == typeof(Uri);
    }

    private static string SerializeSafely(object? value)
    {
        try
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }
        catch (Exception exception)
        {
            return $"<serialize failed: {exception.GetType().Name}: {exception.Message}>";
        }
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaximumStringLength)
        {
            return value;
        }

        return $"{value[..MaximumStringLength]}...<truncated>";
    }
}
