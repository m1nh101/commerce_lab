using System.Globalization;
using System.Text;
using CommerceHub.ProductCatalog.Domain.Categories;

namespace CommerceHub.ProductCatalog.Application.Categories;

internal static class SlugGenerator
{
    /// <summary>
    /// Builds a kebab-case slug, e.g. <c>"Men's Shirts"</c> becomes <c>"mens-shirts"</c>.
    /// </summary>
    public static string Generate(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var pendingSeparator = false;

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || c is '\'' or '’')
            {
                continue;
            }

            var lower = char.ToLowerInvariant(c);
            if (lower is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(lower);
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = true;
            }
        }

        var slug = builder.ToString();
        return slug.Length <= Category.SlugMaxLength
            ? slug
            : slug[..Category.SlugMaxLength].TrimEnd('-');
    }
}
