using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Variants.Commands;

public sealed record CreateVariantCommand(Guid ProductId, VariantFields Fields) : ICommand<Result<VariantDto>>;

internal sealed class CreateVariantCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<CreateVariantCommand, Result<VariantDto>>
{
    public async Task<Result<VariantDto>> HandleAsync(CreateVariantCommand command, CancellationToken cancellationToken = default)
    {
        var validation = VariantValidator.Validate(command.Fields);
        if (validation.Error is { } validationError)
        {
            return validationError;
        }

        var valid = validation.Value;
        var product = await LoadProductAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return VariantErrors.ProductNotFound;
        }

        if (await dbContext.CheckVariantReferencesAsync(valid, excludeId: null, cancellationToken) is { } referenceError)
        {
            return referenceError;
        }

        var variant = AddVariant(product, valid, command.Fields.Status);

        if (await dbContext.SaveVariantChangesAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await dbContext.LoadVariantDtoAsync(variant.Id, cancellationToken);
    }

    private Task<Product?> LoadProductAsync(Guid productId, CancellationToken cancellationToken) =>
        dbContext.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

    private ProductVariant AddVariant(Product product, ValidVariant valid, ProductVariantStatus? status)
    {
        var variant = product.AddVariant(valid.Sku, valid.Name, valid.Price, valid.Currency);
        variant.ChangeStatus(status ?? ProductVariantStatus.Draft);
        foreach (var (attributeId, value) in valid.Attributes)
        {
            variant.SetAttribute(attributeId, value);
        }

        for (var i = 0; i < valid.ImageUrls.Count; i++)
        {
            product.AddImage(valid.ImageUrls[i], i, variant.Id);
        }

        // The variant id is generated client-side, so mark it Added explicitly rather than relying on graph discovery.
        dbContext.ProductVariants.Add(variant);
        return variant;
    }
}
