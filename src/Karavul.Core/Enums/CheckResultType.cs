namespace Karavul.Core.Enums;

public enum CheckResultType
{
    Success = 0,
    HttpError = 1,
    Timeout = 2,
    DnsError = 3,
    ConnectionError = 4,
    ResponseTimeTooHigh = 5,
    SslError = 6,
    UnexpectedStatusCode = 7
}
