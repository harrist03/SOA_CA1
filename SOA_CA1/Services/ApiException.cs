using System.Net;

namespace SOA_CA1.Services;

public sealed class ApiException : Exception
{
    public ApiException(string provider, HttpStatusCode? statusCode, string message)
        : base(message)
    {
        Provider = provider;
        StatusCode = statusCode;
    }

    public string Provider { get; }
    public HttpStatusCode? StatusCode { get; }
}
