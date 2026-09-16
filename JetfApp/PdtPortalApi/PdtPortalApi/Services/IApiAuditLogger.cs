namespace PdtPortalApi.Services;

public interface IApiAuditLogger
{
    void Log(
        string account,
        string httpMethod,
        string path,
        string controller,
        string action,
        int statusCode,
        long elapsedMilliseconds,
        string request,
        string response);
}
