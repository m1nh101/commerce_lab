using System.Text.Json;
using Asp.Versioning;
using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Commands;
using CommerceHub.ProductCatalog.Application.Products.Queries;
using CommerceHub.ProductCatalog.Application.Variants;
using CommerceHub.ProductCatalog.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

namespace CommerceHub.ProductCatalog.Endpoints.Products;

internal static class ProductEndpoints
{
    private const string DescriptionJsonProperty = "description";

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("api/v{version:apiVersion}/products")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Products");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPatch("/{id:guid}", PatchAsync);
        group.MapPost("/{id:guid}/status", ChangeStatusAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
        group.MapPut("/{id:guid}/categories/{categoryId:long}", AssignCategoryAsync);
        group.MapDelete("/{id:guid}/categories/{categoryId:long}", RemoveCategoryAsync);
        group.MapPut("/{id:guid}/categories", ReplaceCategoriesAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request,
        ICommandHandler<CreateProductCommand, Result<ProductDetailDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var fields = new ProductFields(request.Name, request.Slug, request.Description, request.Status);
        var variants = request.Variants?
            .Select(v => v is null
                ? null!
                : new VariantFields(v.Sku, v.Name, v.Price, v.Currency, v.Status,
                    v.Attributes?.Select(a => new VariantAttributeField(a?.AttributeId, a?.Value)).ToList(),
                    v.ImageUrls))
            .ToList();

        var command = new CreateProductCommand(fields, variants, request.CategoryIds);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiResults.Failure(result.Error!);
        }

        var version = httpContext.Request.RouteValues["version"];
        return ApiResults.Created($"/api/v{version}/products/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListProductsQuery, Result<PagedResult<ProductDto>>> handler,
        CancellationToken cancellationToken,
        string? status = null,
        [FromQuery(Name = "category_id")] long? categoryId = null,
        string? search = null,
        string? slug = null,
        string? sort = null,
        int page = 1,
        int limit = 20)
    {
        ProductStatus? statusValue = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(status, out var parsedStatus))
            {
                return ApiResults.ValidationFailure("'status' must be one of draft, active, inactive, archived.");
            }

            statusValue = parsedStatus;
        }

        if (!TryParseSort(sort, out var sortValue))
        {
            return ApiResults.ValidationFailure("'sort' must be one of name, -name, created_at, -created_at.");
        }

        var query = new ListProductsQuery(statusValue, categoryId, search, slug, sortValue, page, limit);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        IQueryHandler<GetProductQuery, Result<ProductDetailDto>> handler,
        CancellationToken cancellationToken,
        string? include = null)
    {
        if (!TryParseIncludes(include, out var includes))
        {
            return ApiResults.ValidationFailure("'include' must be a comma-separated list of variants, categories, images.");
        }

        var result = await handler.HandleAsync(new GetProductQuery(id, includes), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        ICommandHandler<UpdateProductCommand, Result<ProductDto>> handler,
        CancellationToken cancellationToken)
    {
        var fields = new ProductFields(request.Name, request.Slug, request.Description, request.Status);
        var result = await handler.HandleAsync(new UpdateProductCommand(id, fields), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> PatchAsync(
        Guid id,
        JsonElement body,
        ICommandHandler<PatchProductCommand, Result<ProductDto>> handler,
        IOptions<JsonOptions> jsonOptions,
        CancellationToken cancellationToken)
    {
        if (body.ValueKind != JsonValueKind.Object)
        {
            return ApiResults.ValidationFailure("Request body must be a JSON object.");
        }

        UpdateProductRequest? request;
        try
        {
            request = body.Deserialize<UpdateProductRequest>(jsonOptions.Value.SerializerOptions);
        }
        catch (JsonException)
        {
            return ApiResults.ValidationFailure("Request body contains invalid values.");
        }

        // Partial update: only fields present in the body change, so an explicit "description": null clears it.
        var patch = new ProductPatch(
            request?.Name,
            request?.Slug,
            request?.Status,
            DescriptionSpecified: body.TryGetProperty(DescriptionJsonProperty, out _),
            request?.Description);

        var result = await handler.HandleAsync(new PatchProductCommand(id, patch), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeProductStatusRequest request,
        ICommandHandler<ChangeProductStatusCommand, Result<ProductDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ChangeProductStatusCommand(id, request.Status), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ICommandHandler<DeleteProductCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Product deleted successfully.")
            : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> AssignCategoryAsync(
        Guid id,
        long categoryId,
        ICommandHandler<AssignProductCategoryCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AssignProductCategoryCommand(id, categoryId), cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Category assigned successfully.")
            : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> RemoveCategoryAsync(
        Guid id,
        long categoryId,
        ICommandHandler<RemoveProductCategoryCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveProductCategoryCommand(id, categoryId), cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Category removed successfully.")
            : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> ReplaceCategoriesAsync(
        Guid id,
        ReplaceProductCategoriesRequest request,
        ICommandHandler<ReplaceProductCategoriesCommand, Result<IReadOnlyList<ProductCategoryDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReplaceProductCategoriesCommand(id, request.CategoryIds), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static bool TryParseStatus(string value, out ProductStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) &&
        !char.IsDigit(value.Trim()[0]) &&
        Enum.IsDefined(status);

    private static bool TryParseSort(string? value, out ProductSort sort)
    {
        sort = value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "-created_at" => ProductSort.CreatedAtDesc,
            "created_at" => ProductSort.CreatedAtAsc,
            "name" => ProductSort.NameAsc,
            "-name" => ProductSort.NameDesc,
            _ => (ProductSort)(-1),
        };

        return Enum.IsDefined(sort);
    }

    private static bool TryParseIncludes(string? value, out ProductIncludes includes)
    {
        includes = ProductIncludes.None;
        foreach (var part in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "variants": includes |= ProductIncludes.Variants; break;
                case "categories": includes |= ProductIncludes.Categories; break;
                case "images": includes |= ProductIncludes.Images; break;
                default: return false;
            }
        }

        return true;
    }
}
