using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Variants;

/// <summary>
/// Normalized, validated variant fields ready to apply to the domain.
/// </summary>
internal sealed record ValidVariant(
    string Sku,
    string Name,
    decimal Price,
    string Currency,
    IReadOnlyList<(long AttributeId, string Value)> Attributes);

/// <summary>
/// Pure input validation for variants. Checks against stored data (SKU uniqueness, attribute existence) are done by the
/// handlers before any write.
/// </summary>
internal static class VariantValidator
{
    // DECIMAL(12,2): at most 10 integer digits and 2 fractional digits.
    private const decimal MaxPrice = 9_999_999_999.99m;

    public static Result<ValidVariant> Validate(VariantFields fields)
    {
        var (sku, name, price, currency, status, attributes) = fields;

        if (string.IsNullOrWhiteSpace(sku))
        {
            return Error.Validation("'sku' is required.");
        }

        if (sku.Trim().Length > ProductVariant.SkuMaxLength)
        {
            return Error.Validation($"'sku' must not exceed {ProductVariant.SkuMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("'name' is required.");
        }

        if (name.Trim().Length > ProductVariant.NameMaxLength)
        {
            return Error.Validation($"'name' must not exceed {ProductVariant.NameMaxLength} characters.");
        }

        if (price is null)
        {
            return Error.Validation("'price' is required.");
        }

        if (price < 0 || price > MaxPrice || decimal.Round(price.Value, 2) != price.Value)
        {
            return Error.Validation($"'price' must be between 0 and {MaxPrice} with at most 2 decimal places.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Error.Validation("'currency' is required.");
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != ProductVariant.CurrencyLength || !normalizedCurrency.All(char.IsAsciiLetterUpper))
        {
            return Error.Validation("'currency' must be a three-letter ISO 4217 code.");
        }

        if (status is { } s && !Enum.IsDefined(s))
        {
            return Error.Validation("'status' must be one of draft, active, inactive, archived.");
        }

        var values = new List<(long AttributeId, string Value)>();
        foreach (var attribute in attributes ?? [])
        {
            if (attribute?.AttributeId is not { } attributeId)
            {
                return Error.Validation("'attribute_id' is required for every attribute.");
            }

            if (values.Any(v => v.AttributeId == attributeId))
            {
                return Error.Validation($"Attribute '{attributeId}' appears more than once.");
            }

            if (ValidateAttributeValue(attribute.Value) is { } valueError)
            {
                return valueError;
            }

            values.Add((attributeId, attribute.Value!.Trim()));
        }

        return new ValidVariant(sku.Trim(), name.Trim(), price.Value, normalizedCurrency, values);
    }

    public static Error? ValidateAttributeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Error.Validation("Attribute 'value' is required.");
        }

        return value.Trim().Length > ProductVariantAttribute.ValueMaxLength
            ? Error.Validation($"Attribute 'value' must not exceed {ProductVariantAttribute.ValueMaxLength} characters.")
            : null;
    }
}
