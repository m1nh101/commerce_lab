using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Attributes;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Variants.Commands;

public sealed record SetVariantAttributeCommand(Guid Id, long AttributeId, string? Value) : ICommand<Result<VariantDto>>;

/// <summary>
/// Upserts one attribute value; repeating the same command leaves the same state.
/// </summary>
internal sealed class SetVariantAttributeCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<SetVariantAttributeCommand, Result<VariantDto>>
{
    public async Task<Result<VariantDto>> HandleAsync(SetVariantAttributeCommand command, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .Include(v => v.Attributes)
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (variant is null)
        {
            return VariantErrors.NotFound;
        }

        if (!await AttributeExistsAsync(command.AttributeId, cancellationToken))
        {
            return AttributeErrors.NotFound;
        }

        if (VariantValidator.ValidateAttributeValue(command.Value) is { } error)
        {
            return error;
        }

        variant.SetAttribute(command.AttributeId, command.Value!.Trim());

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The attribute definition was deleted concurrently and the FK rejected the value.
            if (!await AttributeExistsAsync(command.AttributeId, cancellationToken))
            {
                return AttributeErrors.NotFound;
            }

            throw;
        }

        return await dbContext.LoadVariantDtoAsync(command.Id, cancellationToken);
    }

    private Task<bool> AttributeExistsAsync(long attributeId, CancellationToken cancellationToken) =>
        dbContext.Attributes.AnyAsync(a => a.Id == attributeId, cancellationToken);
}
