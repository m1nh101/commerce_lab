using System.Text.RegularExpressions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Products;

/// <summary>
/// Normalized, validated product fields ready to apply to the domain.
/// </summary>
internal sealed record ValidProduct(string Name, string Slug, string? Description, ProductStatus Status);

/// <summary>
/// Pure input validation for products. Slug uniqueness is checked by the handlers before any write.
/// </summary>
internal static partial class ProductValidator
{
    public static readonly Error InvalidStatus =
        Error.Validation("'status' must be one of draft, active, inactive, archived.");

    public static Result<ValidProduct> Validate(ProductFields fields)
    {
        var (name, slug, description, status) = fields;

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("'name' is required.");
        }

        if (name.Trim().Length > Product.NameMaxLength)
        {
            return Error.Validation($"'name' must not exceed {Product.NameMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Error.Validation("'slug' is required.");
        }

        var normalizedSlug = slug.Trim();
        if (normalizedSlug.Length > Product.SlugMaxLength)
        {
            return Error.Validation($"'slug' must not exceed {Product.SlugMaxLength} characters.");
        }

        if (!SlugPattern().IsMatch(normalizedSlug))
        {
            return Error.Validation("'slug' must be lowercase alphanumeric words separated by single hyphens.");
        }

        if (status is { } s && !Enum.IsDefined(s))
        {
            return InvalidStatus;
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return new ValidProduct(name.Trim(), normalizedSlug, normalizedDescription, status ?? ProductStatus.Draft);
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
