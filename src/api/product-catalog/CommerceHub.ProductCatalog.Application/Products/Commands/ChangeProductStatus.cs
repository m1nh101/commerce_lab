using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

/// <summary>
/// Any defined status is accepted; a transition matrix is not enforced until the business confirms one.
/// </summary>
public sealed record ChangeProductStatusCommand(Guid Id, ProductStatus? Status) : ICommand<Result<ProductDto>>;

internal sealed class ChangeProductStatusCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<ChangeProductStatusCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> HandleAsync(ChangeProductStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Status is not { } status)
        {
            return Error.Validation("'status' is required.");
        }

        if (!Enum.IsDefined(status))
        {
            return ProductValidator.InvalidStatus;
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        product.ChangeStatus(status);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.LoadProductDtoAsync(product.Id, cancellationToken);
    }
}
