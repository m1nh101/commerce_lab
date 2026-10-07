using Asp.Versioning;
using CommerceHub.ProductCatalog.Application.Attributes;
using CommerceHub.ProductCatalog.Common;

namespace CommerceHub.ProductCatalog.Endpoints.Attributes;

internal static class AttributeEndpoints
{
    public static IEndpointRouteBuilder MapAttributeEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("api/v{version:apiVersion}/attributes")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Attributes");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:long}", GetAsync);
        group.MapPut("/{id:long}", UpdateAsync);
        group.MapDelete("/{id:long}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateAttributeRequest request,
        IAttributeService attributeService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await attributeService.CreateAsync(
            new CreateAttributeInput(request.Name, request.Code),
            cancellationToken);

        return result.IsSuccess
            ? ApiResults.Created($"{httpContext.Request.Path.Value?.TrimEnd('/')}/{result.Value.Id}", result.Value)
            : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> ListAsync(
        IAttributeService attributeService,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1,
        int limit = 20)
    {
        var result = await attributeService.ListAsync(new AttributeListQuery(search, page, limit), cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> GetAsync(
        long id,
        IAttributeService attributeService,
        CancellationToken cancellationToken)
    {
        var result = await attributeService.GetAsync(id, cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        long id,
        UpdateAttributeRequest request,
        IAttributeService attributeService,
        CancellationToken cancellationToken)
    {
        var result = await attributeService.UpdateAsync(
            id,
            new UpdateAttributeInput(request.Name, request.Code),
            cancellationToken);

        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        long id,
        IAttributeService attributeService,
        CancellationToken cancellationToken)
    {
        var result = await attributeService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Attribute deleted successfully.")
            : ApiResults.Failure(result.Error!);
    }
}
