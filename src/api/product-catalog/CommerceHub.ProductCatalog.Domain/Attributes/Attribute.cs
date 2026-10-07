using CommerceHub.ProductCatalog.Domain.Common;

namespace CommerceHub.ProductCatalog.Domain.Attributes;

public sealed class Attribute : Entity<long>, IAggregateRoot
{
    public const int NameMaxLength = 100;
    public const int CodeMaxLength = 100;

    private Attribute()
    {
    }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public static Attribute Create(string name, string code)
    {
        return new Attribute
        {
            Name = Guard.NotEmpty(name, NameMaxLength, nameof(name)),
            Code = Guard.NotEmpty(code, CodeMaxLength, nameof(code)).ToLowerInvariant()
        };
    }

    public void Rename(string name) => Name = Guard.NotEmpty(name, NameMaxLength, nameof(name));

    public void UpdateDetails(string name, string code)
    {
        Name = Guard.NotEmpty(name, NameMaxLength, nameof(name));
        Code = Guard.NotEmpty(code, CodeMaxLength, nameof(code)).ToLowerInvariant();
    }
}
