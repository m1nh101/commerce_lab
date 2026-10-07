using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Variants.Commands;

public sealed record UpdateVariantCommand(Guid Id, VariantFields Fields) : ICommand<Result<VariantDto>>;

internal sealed class UpdateVariantCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<UpdateVariantCommand, Result<VariantDto>>
{
    public async Task<Result<VariantDto>> HandleAsync(UpdateVariantCommand command, CancellationToken cancellationToken = default)
    {
        var validation = VariantValidator.Validate(command.Fields);
        if (validation.Error is { } validationError)
        {
            return validationError;
        }

        var valid = validation.Value;
        var variant = await dbContext.ProductVariants
            .Include(v => v.Attributes)
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (variant is null)
        {
            return VariantErrors.NotFound;
        }

        if (await dbContext.CheckVariantReferencesAsync(valid, command.Id, cancellationToken) is { } referenceError)
        {
            return referenceError;
        }

        ApplyChanges(variant, valid, command.Fields);

        if (await dbContext.SaveVariantChangesAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return await dbContext.LoadVariantDtoAsync(command.Id, cancellationToken);
    }

    private static void ApplyChanges(ProductVariant variant, ValidVariant valid, VariantFields fields)
    {
        variant.ChangeSku(valid.Sku);
        variant.Rename(valid.Name);
        variant.ChangePrice(valid.Price, valid.Currency);
        if (fields.Status is { } status)
        {
            variant.ChangeStatus(status);
        }

        // When provided, the attribute list replaces the variant's full set of values.
        if (fields.Attributes is not null)
        {
            ReplaceAttributes(variant, valid.Attributes);
        }
    }

    private static void ReplaceAttributes(ProductVariant variant, IReadOnlyList<(long AttributeId, string Value)> attributes)
    {
        var requested = attributes.Select(a => a.AttributeId).ToHashSet();
        var removed = variant.Attributes.Where(a => !requested.Contains(a.AttributeId)).Select(a => a.AttributeId).ToList();
        foreach (var attributeId in removed)
        {
            variant.RemoveAttribute(attributeId);
        }

        foreach (var (attributeId, value) in attributes)
        {
            variant.SetAttribute(attributeId, value);
        }
    }
}
