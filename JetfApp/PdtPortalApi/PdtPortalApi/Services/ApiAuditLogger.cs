using NLog;

namespace PdtPortalApi.Services;

public sealed class ApiAuditLogger : IApiAuditLogger
{
    private const string AuditLoggerName = "ApiRequestTrace";
    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly Logger _logger;
    private readonly ILogger<ApiAuditLogger> _systemLogger;

    public ApiAuditLogger(ILogger<ApiAuditLogger> systemLogger)
    {
        _systemLogger = systemLogger;
        _logger = LogManager.GetLogger(AuditLoggerName);
    }

    public void LogRequestBegin(
        string account,
        string httpMethod,
        string path,
        string request)
    {
        Write(
            account,
            path,
            $"Request_Begin[Debug] - [{httpMethod}] {path} | Params={request}");
    }

    public void LogRequestEnd(
        string account,
        string httpMethod,
        string path,
        long elapsedMilliseconds)
    {
        Write(
            account,
            path,
            $"Request_End[Debug] - [{httpMethod}] {path} | cost: {elapsedMilliseconds}ms");
    }

    private void Write(string account, string path, string message)
    {
        try
        {
            var normalizedAccount = string.IsNullOrWhiteSpace(account) ? "Unknown" : account.Trim();
            var logEvent = new LogEventInfo(NLog.LogLevel.Debug, AuditLoggerName, message);
            logEvent.Properties["AccountFileName"] = SanitizeFileName(normalizedAccount);
            _logger.Log(logEvent);
        }
        catch (Exception exception)
        {
            _systemLogger.LogError(
                exception,
                "寫入 API 稽核日誌失敗，Account: {Account}, Path: {Path}",
                account,
                path);
        }
    }

    private static string SanitizeFileName(string account)
    {
        var sanitizedCharacters = account
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.' ? character : '_')
            .Take(80)
            .ToArray();
        var sanitized = new string(sanitizedCharacters).Trim('.');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "Unknown";
        }

        return ReservedFileNames.Contains(sanitized) ? $"_{sanitized}" : sanitized;
    }
}
