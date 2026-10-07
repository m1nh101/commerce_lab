using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Attributes;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Application.Variants;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

/// <summary>
/// Creates a product, optionally with its variants and category assignments, in a single transaction.
/// Every part is validated before anything is written; if any part is invalid nothing is persisted.
/// </summary>
public sealed record CreateProductCommand(
    ProductFields Fields,
    IReadOnlyList<VariantFields>? Variants = null,
    IReadOnlyList<long>? CategoryIds = null) : ICommand<Result<ProductDetailDto>>;

internal sealed class CreateProductCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<CreateProductCommand, Result<ProductDetailDto>>
{
    public async Task<Result<ProductDetailDto>> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        var validation = ProductValidator.Validate(command.Fields);
        if (validation.Error is { } validationError)
        {
            return validationError;
        }

        var variantsValidation = ValidateVariants(command.Variants ?? []);
        if (variantsValidation.Error is { } variantError)
        {
            return variantError;
        }

        var valid = validation.Value;
        var variants = variantsValidation.Value;
        var categoryIds = (command.CategoryIds ?? []).Distinct().ToList();

        if (await CheckReferencesAsync(valid, variants, categoryIds, cancellationToken) is { } referenceError)
        {
            return referenceError;
        }

        var product = Product.Create(valid.Name, valid.Slug, valid.Description);
        product.ChangeStatus(valid.Status);

        foreach (var (variantValid, status) in variants)
        {
            var variant = product.AddVariant(variantValid.Sku, variantValid.Name, variantValid.Price, variantValid.Currency);
            variant.ChangeStatus(status ?? ProductVariantStatus.Draft);
            foreach (var (attributeId, value) in variantValid.Attributes)
            {
                variant.SetAttribute(attributeId, value);
            }

            for (var i = 0; i < variantValid.ImageUrls.Count; i++)
            {
                product.AddImage(variantValid.ImageUrls[i], i, variant.Id);
            }
        }

        foreach (var categoryId in categoryIds)
        {
            product.AssignCategory(categoryId);
        }

        // Adding the root marks the whole graph (variants, attribute values, images, category mappings) as Added.
        dbContext.Products.Add(product);

        if (await dbContext.SaveProductChangesAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await dbContext.LoadProductDetailAsync(
            product.Id,
            ProductIncludes.Variants | ProductIncludes.Categories,
            cancellationToken);
    }

    private static Result<List<(ValidVariant Valid, ProductVariantStatus? Status)>> ValidateVariants(
        IReadOnlyList<VariantFields> variants)
    {
        var result = new List<(ValidVariant, ProductVariantStatus?)>();
        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < variants.Count; i++)
        {
            var fields = variants[i];
            if (fields is null)
            {
                return Error.Validation($"variants[{i}]: variant must not be null.");
            }

            var validation = VariantValidator.Validate(fields);
            if (validation.Error is { } error)
            {
                return error with { Message = $"variants[{i}]: {error.Message}" };
            }

            if (!skus.Add(validation.Value.Sku))
            {
                return Error.Validation($"variants[{i}]: SKU '{validation.Value.Sku}' appears more than once.");
            }

            result.Add((validation.Value, fields.Status));
        }

        return result;
    }

    private async Task<Error?> CheckReferencesAsync(
        ValidProduct product,
        List<(ValidVariant Valid, ProductVariantStatus? Status)> variants,
        List<long> categoryIds,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AsNoTracking().SlugExistsAsync(product.Slug, excludeId: null, cancellationToken))
        {
            return ProductErrors.SlugConflict;
        }

        var skus = variants.Select(v => v.Valid.Sku).ToList();
        if (await dbContext.ProductVariants.AsNoTracking().AnySkuExistsAsync(skus, cancellationToken))
        {
            return VariantErrors.SkuConflict;
        }

        var attributeIds = variants.SelectMany(v => v.Valid.Attributes).Select(a => a.AttributeId).Distinct().ToList();
        if (!await dbContext.Attributes.AsNoTracking().AllExistAsync(attributeIds, cancellationToken))
        {
            return AttributeErrors.NotFound;
        }

        if (categoryIds.Count > 0 &&
            await dbContext.Categories.CountAsync(c => categoryIds.Contains(c.Id), cancellationToken) != categoryIds.Count)
        {
            return ProductErrors.CategoryNotFound;
        }

        return null;
    }
}
