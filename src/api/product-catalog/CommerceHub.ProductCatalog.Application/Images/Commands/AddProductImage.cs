using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Images.Commands;

public sealed record AddProductImageCommand(Guid ProductId, ImageFields Fields) : ICommand<Result<ImageDto>>;

internal sealed class AddProductImageCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<AddProductImageCommand, Result<ImageDto>>
{
    public async Task<Result<ImageDto>> HandleAsync(AddProductImageCommand command, CancellationToken cancellationToken = default)
    {
        var url = ImageValidator.ValidateUrl(command.Fields.Url);
        if (url.Error is { } validationError)
        {
            return validationError;
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return ImageErrors.ProductNotFound;
        }

        var image = product.AddImage(url.Value, command.Fields.SortOrder ?? 0);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ImageDto.From(image);
    }
}
