using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Images.Commands;

public sealed record AddVariantImageCommand(Guid VariantId, ImageFields Fields) : ICommand<Result<ImageDto>>;

internal sealed class AddVariantImageCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<AddVariantImageCommand, Result<ImageDto>>
{
    public async Task<Result<ImageDto>> HandleAsync(AddVariantImageCommand command, CancellationToken cancellationToken = default)
    {
        var url = ImageValidator.ValidateUrl(command.Fields.Url);
        if (url.Error is { } validationError)
        {
            return validationError;
        }

        var productId = await dbContext.ProductVariants
            .Where(v => v.Id == command.VariantId)
            .Select(v => (Guid?)v.ProductId)
            .FirstOrDefaultAsync(cancellationToken);
        if (productId is null)
        {
            return ImageErrors.VariantNotFound;
        }

        // Resolve the variant's parent so the image stores both ids; Product.AddImage enforces that the variant belongs to it.
        var product = await dbContext.Products
            .Include(p => p.Variants.Where(v => v.Id == command.VariantId))
            .FirstAsync(p => p.Id == productId, cancellationToken);

        var image = product.AddImage(url.Value, command.Fields.SortOrder ?? 0, command.VariantId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ImageDto.From(image);
    }
}
