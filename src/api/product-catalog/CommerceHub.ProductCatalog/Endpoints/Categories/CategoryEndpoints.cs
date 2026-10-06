using System.Text.Json;
using Asp.Versioning;
using CommerceHub.ProductCatalog.Application.Categories;
using CommerceHub.ProductCatalog.Common;
using CommerceHub.ProductCatalog.Domain.Categories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace CommerceHub.ProductCatalog.Endpoints.Categories;

internal static class CategoryEndpoints
{
    private const string ParentIdQueryKey = "parent_id";
    private const string ParentIdJsonProperty = "parent_id";

    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("api/v{version:apiVersion}/categories")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Categories");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{idOrSlug}", GetAsync);
        group.MapPut("/{id:long}", PutAsync);
        group.MapPatch("/{id:long}", PatchAsync);
        group.MapDelete("/{id:long}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateCategoryRequest request,
        ICategoryService categoryService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.CreateAsync(
            new CreateCategoryInput(request.Name, request.Slug, request.ParentId, request.Status),
            cancellationToken);

        return result.IsSuccess
            ? ApiResults.Created($"{httpContext.Request.Path.Value?.TrimEnd('/')}/{result.Value.Id}", result.Value)
            : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> ListAsync(
        ICategoryService categoryService,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        [FromQuery(Name = ParentIdQueryKey)] string? parentId,
        bool tree = false,
        string? status = null,
        string? search = null,
        int page = 1,
        int limit = 20)
    {
        // "?parent_id=" or "?parent_id=null" filters top-level categories; an absent key means no parent filter.
        var parentIdSpecified = httpContext.Request.Query.ContainsKey(ParentIdQueryKey);
        long? parentIdValue = null;
        if (parentIdSpecified && !string.IsNullOrWhiteSpace(parentId) &&
            !parentId.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            if (!long.TryParse(parentId, out var parsed))
            {
                return ApiResults.ValidationFailure("'parent_id' must be an integer or null.");
            }

            parentIdValue = parsed;
        }

        CategoryStatus? statusValue = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(status, out var parsedStatus))
            {
                return ApiResults.ValidationFailure("'status' must be one of: active, hidden, archived.");
            }

            statusValue = parsedStatus;
        }

        var query = new CategoryListQuery(parentIdSpecified, parentIdValue, statusValue, search, page, limit);

        if (tree)
        {
            return ApiResults.Ok(await categoryService.GetTreeAsync(query, cancellationToken));
        }

        var result = await categoryService.ListAsync(query, cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> GetAsync(
        string idOrSlug,
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.GetAsync(idOrSlug, cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> PutAsync(
        long id,
        UpdateCategoryRequest request,
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        // Full update: name is required and parent_id is always applied (null moves the category to the root).
        var input = new UpdateCategoryInput(
            request.Name ?? string.Empty,
            request.Slug,
            request.Status,
            ParentIdSpecified: true,
            request.ParentId);

        return await UpdateAsync(id, input, categoryService, cancellationToken);
    }

    private static async Task<IResult> PatchAsync(
        long id,
        JsonElement body,
        ICategoryService categoryService,
        IOptions<JsonOptions> jsonOptions,
        CancellationToken cancellationToken)
    {
        if (body.ValueKind != JsonValueKind.Object)
        {
            return ApiResults.ValidationFailure("Request body must be a JSON object.");
        }

        UpdateCategoryRequest? request;
        try
        {
            request = body.Deserialize<UpdateCategoryRequest>(jsonOptions.Value.SerializerOptions);
        }
        catch (JsonException)
        {
            return ApiResults.ValidationFailure("Request body contains invalid values.");
        }

        // Partial update: only fields present in the body change, so an explicit "parent_id": null moves to the root.
        var input = new UpdateCategoryInput(
            request?.Name,
            request?.Slug,
            request?.Status,
            ParentIdSpecified: body.TryGetProperty(ParentIdJsonProperty, out _),
            request?.ParentId);

        return await UpdateAsync(id, input, categoryService, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        long id,
        ICategoryService categoryService,
        CancellationToken cancellationToken,
        [FromQuery(Name = "reassign_children_to")] long? reassignChildrenTo)
    {
        var result = await categoryService.DeleteAsync(id, reassignChildrenTo, cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Category deleted successfully.")
            : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        long id,
        UpdateCategoryInput input,
        ICategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result = await categoryService.UpdateAsync(id, input, cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static bool TryParseStatus(string value, out CategoryStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) &&
        !char.IsDigit(value.Trim()[0]) &&
        Enum.IsDefined(status);
}
