using System.Text.Json.Serialization;
using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Common;

public sealed record ApiError(string Code, string Message);

public sealed record PaginationMeta(int Total, int Page, int Limit, int TotalPages);

/// <summary>
/// Standard response envelope: <c>{ success, data, pagination, message, error }</c>. Absent sections are omitted.
/// </summary>
public sealed record ApiResponse(
    bool Success,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Data = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PaginationMeta? Pagination = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ApiError? Error = null);

internal static class ApiResults
{
    public static IResult Ok(object data) => Results.Ok(new ApiResponse(true, data));

    public static IResult Ok<T>(PagedResult<T> page) =>
        Results.Ok(new ApiResponse(
            true,
            page.Items,
            new PaginationMeta(page.Total, page.Page, page.Limit, page.TotalPages)));

    public static IResult Created(string location, object data) =>
        Results.Created(location, new ApiResponse(true, data));

    public static IResult Message(string message) => Results.Ok(new ApiResponse(true, Message: message));

    public static IResult Failure(Error error) =>
        Results.Json(
            new ApiResponse(false, Error: new ApiError(error.Code, error.Message)),
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unprocessable => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status400BadRequest
            });

    public static IResult ValidationFailure(string message) => Failure(Error.Validation(message));
}
