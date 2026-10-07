using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

public sealed record PatchProductCommand(Guid Id, ProductPatch Patch) : ICommand<Result<ProductDto>>;

internal sealed class PatchProductCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<PatchProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> HandleAsync(PatchProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        // Merge onto the current state, then validate the result exactly like a full replace.
        var patch = command.Patch;
        var merged = new ProductFields(
            patch.Name ?? product.Name,
            patch.Slug ?? product.Slug,
            patch.DescriptionSpecified ? patch.Description : product.Description,
            patch.Status ?? product.Status);

        var validation = ProductValidator.Validate(merged);
        if (validation.Error is { } validationError)
        {
            return validationError;
        }

        return await dbContext.ApplyProductChangesAsync(product, validation.Value, cancellationToken);
    }
}
