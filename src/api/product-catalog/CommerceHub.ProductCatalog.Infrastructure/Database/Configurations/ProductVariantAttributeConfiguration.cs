using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal sealed class ProductVariantAttributeConfiguration : IEntityTypeConfiguration<ProductVariantAttribute>
{
    public void Configure(EntityTypeBuilder<ProductVariantAttribute> builder)
    {
        builder.ToTable("product_variant_attributes");

        builder.HasKey(a => new { a.VariantId, a.AttributeId });

        builder.Property(a => a.Value)
            .HasMaxLength(ProductVariantAttribute.ValueMaxLength)
            .IsRequired();

        // Attribute is a separate aggregate: keep the FK, but no navigation property.
        builder.HasOne<ProductAttribute>()
            .WithMany()
            .HasForeignKey(a => a.AttributeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
