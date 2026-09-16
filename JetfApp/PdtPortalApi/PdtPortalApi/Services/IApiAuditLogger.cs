namespace PdtPortalApi.Services;

public interface IApiAuditLogger
{
    void LogRequestBegin(
        string account,
        string httpMethod,
        string path,
        string request);

    void LogRequestEnd(
        string account,
        string httpMethod,
        string path,
        long elapsedMilliseconds);
}
