using Asp.Versioning;
using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Application.Variants;
using CommerceHub.ProductCatalog.Application.Variants.Commands;
using CommerceHub.ProductCatalog.Application.Variants.Queries;
using CommerceHub.ProductCatalog.Common;
using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Endpoints.Variants;

internal static class VariantEndpoints
{
    public static IEndpointRouteBuilder MapVariantEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var root = app.MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Variants");

        root.MapPost("/products/{productId:guid}/variants", CreateAsync);

        var group = root.MapGroup("/variants");
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapPost("/{id:guid}/status", ChangeStatusAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);
        group.MapPut("/{id:guid}/attributes/{attributeId:long}", SetAttributeAsync);
        group.MapDelete("/{id:guid}/attributes/{attributeId:long}", RemoveAttributeAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        Guid productId,
        CreateVariantRequest request,
        ICommandHandler<CreateVariantCommand, Result<VariantDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var fields = ToFields(request.Sku, request.Name, request.Price, request.Currency, request.Status, request.Attributes);
        var result = await handler.HandleAsync(new CreateVariantCommand(productId, fields), cancellationToken);

        if (!result.IsSuccess)
        {
            return ApiResults.Failure(result.Error!);
        }

        var version = httpContext.Request.RouteValues["version"];
        return ApiResults.Created($"/api/v{version}/variants/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        IQueryHandler<GetVariantQuery, Result<VariantDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetVariantQuery(id), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateVariantRequest request,
        ICommandHandler<UpdateVariantCommand, Result<VariantDto>> handler,
        CancellationToken cancellationToken)
    {
        var fields = ToFields(request.Sku, request.Name, request.Price, request.Currency, request.Status, request.Attributes);
        var result = await handler.HandleAsync(new UpdateVariantCommand(id, fields), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid id,
        ChangeVariantStatusRequest request,
        ICommandHandler<ChangeVariantStatusCommand, Result<VariantDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ChangeVariantStatusCommand(id, request.Status), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ICommandHandler<DeleteVariantCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteVariantCommand(id), cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Variant deleted successfully.")
            : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> SetAttributeAsync(
        Guid id,
        long attributeId,
        SetVariantAttributeRequest request,
        ICommandHandler<SetVariantAttributeCommand, Result<VariantDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SetVariantAttributeCommand(id, attributeId, request.Value), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> RemoveAttributeAsync(
        Guid id,
        long attributeId,
        ICommandHandler<RemoveVariantAttributeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveVariantAttributeCommand(id, attributeId), cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Variant attribute removed successfully.")
            : ApiResults.Failure(result.Error!);
    }

    private static VariantFields ToFields(
        string? sku,
        string? name,
        decimal? price,
        string? currency,
        ProductVariantStatus? status,
        IReadOnlyList<VariantAttributeRequest>? attributes) =>
        new(sku, name, price, currency, status,
            attributes?.Select(a => new VariantAttributeField(a?.AttributeId, a?.Value)).ToList());
}
