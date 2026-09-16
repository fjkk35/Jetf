using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
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
        var httpMethod = context.HttpContext.Request.Method;
        var path = $"{context.HttpContext.Request.PathBase}{context.HttpContext.Request.Path}";

        _apiAuditLogger.LogRequestBegin(account, httpMethod, path, requestPayload);

        try
        {
            await next();
        }
        finally
        {
            stopwatch.Stop();
            try
            {
                _apiAuditLogger.LogRequestEnd(
                    account,
                    httpMethod,
                    path,
                    stopwatch.ElapsedMilliseconds);
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
