using FluentResults;

namespace DiscoWeb.Errors;

public class NotFoundError : Error
{
    public string ErrorCode { get; }
    public NotFoundError(string message, string errorCode = "404") : base(message)
    {
        ErrorCode = errorCode;
        Metadata.Add("HttpStatusCode", 404);
    }
}

public class ValidationError : Error
{
    public string ErrorCode { get; }
    public ValidationError(string message, string errorCode = "400") : base(message)
    {
        ErrorCode = errorCode;
        Metadata.Add("HttpStatusCode", 400);
    }
}

public class InternalServerError : Error
{
    public string ErrorCode { get; }
    public InternalServerError(string message, string errorCode = "500") : base(message)
    {
        ErrorCode = errorCode;
        Metadata.Add("HttpStatusCode", 500);
    }
}