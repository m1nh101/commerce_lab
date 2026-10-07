using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Images.Commands;

public sealed record UpdateImageCommand(Guid ProductId, long ImageId, ImageFields Fields) : ICommand<Result<ImageDto>>;

internal sealed class UpdateImageCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<UpdateImageCommand, Result<ImageDto>>
{
    public async Task<Result<ImageDto>> HandleAsync(UpdateImageCommand command, CancellationToken cancellationToken = default)
    {
        var url = ImageValidator.ValidateUrl(command.Fields.Url);
        if (url.Error is { } validationError)
        {
            return validationError;
        }

        // The image must belong to the product in the route; its product/variant scope is never changed here.
        var image = await dbContext.ProductImages.FirstOrDefaultAsync(
            i => i.Id == command.ImageId && i.ProductId == command.ProductId, cancellationToken);
        if (image is null)
        {
            return ImageErrors.NotFound;
        }

        image.ChangeUrl(url.Value);
        if (command.Fields.SortOrder is { } sortOrder)
        {
            image.ChangeSortOrder(sortOrder);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ImageDto.From(image);
    }
}
