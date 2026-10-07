using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

/// <summary>
/// Replaces all product fields (PUT). An omitted status resets to <see cref="ProductStatus.Draft"/>, like on create.
/// </summary>
public sealed record UpdateProductCommand(Guid Id, ProductFields Fields) : ICommand<Result<ProductDto>>;

internal sealed class UpdateProductCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<UpdateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var validation = ProductValidator.Validate(command.Fields);
        if (validation.Error is { } validationError)
        {
            return validationError;
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        return await dbContext.ApplyProductChangesAsync(product, validation.Value, cancellationToken);
    }
}

internal static class ProductWriteExtensions
{
    /// <summary>
    /// Applies validated fields to a tracked product, checking slug uniqueness first, then saves and reloads the dto.
    /// </summary>
    public static async Task<Result<ProductDto>> ApplyProductChangesAsync(
        this IProductCatalogDbContext dbContext,
        Product product,
        ValidProduct valid,
        CancellationToken cancellationToken)
    {
        if (valid.Slug != product.Slug &&
            await dbContext.Products.SlugExistsAsync(valid.Slug, product.Id, cancellationToken))
        {
            return ProductErrors.SlugConflict;
        }

        product.UpdateDetails(valid.Name, valid.Slug, valid.Description);
        product.ChangeStatus(valid.Status);

        if (await dbContext.SaveProductChangesAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await dbContext.LoadProductDtoAsync(product.Id, cancellationToken);
    }
}
