namespace TaxPortalApi.Infrastructure.Exceptions;

public sealed class ApiException(int statusCode, string message, string errorCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;
}