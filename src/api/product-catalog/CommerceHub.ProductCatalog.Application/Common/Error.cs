namespace CommerceHub.ProductCatalog.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unprocessable
}

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string message) => new("VALIDATION_ERROR", message, ErrorType.Validation);
}
