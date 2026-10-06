using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal sealed class AttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("attributes");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(a => a.Name)
            .HasMaxLength(ProductAttribute.NameMaxLength)
            .IsRequired();

        builder.Property(a => a.Code)
            .HasMaxLength(ProductAttribute.CodeMaxLength)
            .IsRequired();
        builder.HasIndex(a => a.Code)
            .IsUnique();
    }
}
