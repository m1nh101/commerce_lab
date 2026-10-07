using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Application.Attributes;

internal sealed class AttributeService(IProductCatalogDbContext dbContext) : IAttributeService
{
    private const int MaxPageSize = 100;

    public async Task<Result<AttributeDto>> CreateAsync(CreateAttributeInput input, CancellationToken cancellationToken = default)
    {
        if (Validate(input.Name, input.Code) is { } error)
        {
            return error;
        }

        var code = NormalizeCode(input.Code!);
        if (await dbContext.Attributes.CodeExistsAsync(code, excludeId: null, cancellationToken))
        {
            return AttributeErrors.CodeConflict;
        }

        var attribute = ProductAttribute.Create(input.Name!.Trim(), code);
        dbContext.Attributes.Add(attribute);

        if (await TrySaveAsync(code, excludeId: null, cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return attribute.ToDto();
    }

    public async Task<Result<PagedResult<AttributeDto>>> ListAsync(AttributeListQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
        {
            return Error.Validation("'page' must be greater than or equal to 1.");
        }

        if (query.Limit is < 1 or > MaxPageSize)
        {
            return Error.Validation($"'limit' must be between 1 and {MaxPageSize}.");
        }

        var filtered = dbContext.Attributes.AsNoTracking().WhereSearch(query.Search);
        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToDto()
            .ToListAsync(cancellationToken);

        return new PagedResult<AttributeDto>(items, total, query.Page, query.Limit);
    }

    public async Task<Result<AttributeDto>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var attribute = await dbContext.Attributes
            .AsNoTracking()
            .Where(a => a.Id == id)
            .ToDto()
            .FirstOrDefaultAsync(cancellationToken);

        return attribute is null ? AttributeErrors.NotFound : (Result<AttributeDto>)attribute;
    }

    public async Task<Result<AttributeDto>> UpdateAsync(long id, UpdateAttributeInput input, CancellationToken cancellationToken = default)
    {
        var attribute = await dbContext.Attributes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (attribute is null)
        {
            return AttributeErrors.NotFound;
        }

        if (Validate(input.Name, input.Code) is { } error)
        {
            return error;
        }

        var code = NormalizeCode(input.Code!);
        if (code != attribute.Code &&
            await dbContext.Attributes.CodeExistsAsync(code, id, cancellationToken))
        {
            return AttributeErrors.CodeConflict;
        }

        // Variant values reference the attribute by id, so changing name/code never rewrites them.
        attribute.UpdateDetails(input.Name!.Trim(), code);

        if (await TrySaveAsync(code, id, cancellationToken) is { } saveError)
        {
            return saveError;
        }

        return attribute.ToDto();
    }

    public async Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var attribute = await dbContext.Attributes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (attribute is null)
        {
            return AttributeErrors.NotFound;
        }

        if (await IsInUseAsync(id, cancellationToken))
        {
            return AttributeErrors.InUse;
        }

        dbContext.Attributes.Remove(attribute);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A variant was assigned this attribute concurrently and the restrict FK rejected the delete.
            if (await IsInUseAsync(id, cancellationToken))
            {
                return AttributeErrors.InUse;
            }

            throw;
        }

        return Result.Success();
    }

    private Task<bool> IsInUseAsync(long id, CancellationToken cancellationToken) =>
        dbContext.ProductVariantAttributes.AnyAsync(v => v.AttributeId == id, cancellationToken);

    /// <summary>
    /// Saves changes; the unique index is authoritative, so a violation lost to a concurrent writer becomes a conflict.
    /// </summary>
    private async Task<Error?> TrySaveAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            if (await dbContext.Attributes.AsNoTracking().CodeExistsAsync(code, excludeId, cancellationToken))
            {
                return AttributeErrors.CodeConflict;
            }

            throw;
        }
    }

    private static string NormalizeCode(string code) => code.Trim().ToLowerInvariant();

    private static Error? Validate(string? name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("'name' is required.");
        }

        if (name.Trim().Length > ProductAttribute.NameMaxLength)
        {
            return Error.Validation($"'name' must not exceed {ProductAttribute.NameMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Error.Validation("'code' is required.");
        }

        return code.Trim().Length > ProductAttribute.CodeMaxLength
            ? Error.Validation($"'code' must not exceed {ProductAttribute.CodeMaxLength} characters.")
            : null;
    }
}
