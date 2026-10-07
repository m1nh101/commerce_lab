using Asp.Versioning;
using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Application.Images;
using CommerceHub.ProductCatalog.Application.Images.Commands;
using CommerceHub.ProductCatalog.Common;

namespace CommerceHub.ProductCatalog.Endpoints.Images;

internal static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var root = app.MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1, 0)
            .WithTags("Images");

        root.MapPost("/variants/{variantId:guid}/images", AddVariantImageAsync);

        var group = root.MapGroup("/products/{productId:guid}/images");
        group.MapPost("/upload-url", CreateUploadUrlAsync);
        group.MapPost("/", AddProductImageAsync);
        group.MapPut("/{imageId:long}", UpdateAsync);
        group.MapDelete("/{imageId:long}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> CreateUploadUrlAsync(
        Guid productId,
        CreateImageUploadUrlRequest request,
        ICommandHandler<CreateImageUploadUrlCommand, Result<UploadUrlDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateImageUploadUrlCommand(productId, request.FileName, request.ContentType, request.ContentLength);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> AddProductImageAsync(
        Guid productId,
        AddImageRequest request,
        ICommandHandler<AddProductImageCommand, Result<ImageDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new AddProductImageCommand(productId, new ImageFields(request.Url, request.SortOrder));
        var result = await handler.HandleAsync(command, cancellationToken);
        return ToCreated(result, httpContext);
    }

    private static async Task<IResult> AddVariantImageAsync(
        Guid variantId,
        AddImageRequest request,
        ICommandHandler<AddVariantImageCommand, Result<ImageDto>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new AddVariantImageCommand(variantId, new ImageFields(request.Url, request.SortOrder));
        var result = await handler.HandleAsync(command, cancellationToken);
        return ToCreated(result, httpContext);
    }

    private static async Task<IResult> UpdateAsync(
        Guid productId,
        long imageId,
        UpdateImageRequest request,
        ICommandHandler<UpdateImageCommand, Result<ImageDto>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateImageCommand(productId, imageId, new ImageFields(request.Url, request.SortOrder));
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? ApiResults.Ok(result.Value) : ApiResults.Failure(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        Guid productId,
        long imageId,
        ICommandHandler<DeleteImageCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteImageCommand(productId, imageId), cancellationToken);
        return result.IsSuccess
            ? ApiResults.Message("Image deleted successfully.")
            : ApiResults.Failure(result.Error!);
    }

    private static IResult ToCreated(Result<ImageDto> result, HttpContext httpContext)
    {
        if (!result.IsSuccess)
        {
            return ApiResults.Failure(result.Error!);
        }

        var version = httpContext.Request.RouteValues["version"];
        var image = result.Value;
        return ApiResults.Created($"/api/v{version}/products/{image.ProductId}/images/{image.Id}", image);
    }
}
