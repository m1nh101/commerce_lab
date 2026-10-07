using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

public sealed record DeleteProductCommand(Guid Id) : ICommand<Result>;

/// <summary>
/// Deletes a product in a single statement; the database cascades to variants, variant attributes, category mappings
/// and images. Categories and stock data (owned by another service) are never touched.
/// </summary>
internal sealed class DeleteProductCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<DeleteProductCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.Products
            .Where(p => p.Id == command.Id)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 0 ? ProductErrors.NotFound : Result.Success();
    }
}
