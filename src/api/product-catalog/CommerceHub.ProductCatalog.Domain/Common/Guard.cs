namespace CommerceHub.ProductCatalog.Domain.Common;

internal static class Guard
{
    public static string NotEmpty(string value, int maxLength, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Value must not exceed {maxLength} characters.", paramName);
        }

        return trimmed;
    }
}
