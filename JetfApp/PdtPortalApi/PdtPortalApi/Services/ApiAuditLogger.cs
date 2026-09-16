using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

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

    public ApiAuditLogger(
        IConfiguration configuration,
        ILogger<ApiAuditLogger> systemLogger)
    {
        _systemLogger = systemLogger;
        var logDirectory = ResolveLogDirectory(configuration["FileLogging:Path"]);
        var fileTarget = new FileTarget("apiAccountFile")
        {
            FileName = Path.Combine(
                logDirectory,
                "${shortdate}",
                "${event-properties:item=AccountFileName}.log"),
            Layout = "${date:format=HH\\:mm\\:ss.ffff} | [${threadid}] | ${message}",
            Encoding = Encoding.UTF8,
            KeepFileOpen = false
        };

        var nlogConfiguration = new LoggingConfiguration();
        nlogConfiguration.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget, AuditLoggerName);
        LogManager.Configuration = nlogConfiguration;
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

    private static string ResolveLogDirectory(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "logs");
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
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
