using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommerceHub.ProductCatalog.Application.Images.Commands;

public sealed record CreateImageUploadUrlCommand(
    Guid ProductId,
    string? FileName,
    string? ContentType,
    long? ContentLength) : ICommand<Result<UploadUrlDto>>;

internal sealed class CreateImageUploadUrlCommandHandler(
    IProductCatalogDbContext dbContext,
    IProductImageStorage storage,
    IOptions<ImageUploadOptions> options)
    : ICommandHandler<CreateImageUploadUrlCommand, Result<UploadUrlDto>>
{
    public async Task<Result<UploadUrlDto>> HandleAsync(CreateImageUploadUrlCommand command, CancellationToken cancellationToken = default)
    {
        var validation = ImageValidator.ValidateUpload(
            command.FileName, command.ContentType, command.ContentLength, options.Value.MaxContentLength);
        if (validation.Error is { } validationError)
        {
            return validationError;
        }

        if (!await dbContext.Products.AnyAsync(p => p.Id == command.ProductId, cancellationToken))
        {
            return ImageErrors.ProductNotFound;
        }

        // Issuing a URL creates no product_images row; the client registers the image after uploading.
        var (contentType, extension) = validation.Value;
        var upload = storage.CreateUploadUrl(command.ProductId, extension, contentType, command.ContentLength!.Value);

        return new UploadUrlDto(upload.UploadUrl, upload.Method, upload.Headers, upload.ObjectKey, upload.ImageUrl, upload.ExpiresAt);
    }
}
