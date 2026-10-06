using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasDefaultValueSql("uuidv7()");

        builder.Property(v => v.ProductId)
            .IsRequired();

        builder.Property(v => v.Sku)
            .HasMaxLength(ProductVariant.SkuMaxLength)
            .IsRequired();
        builder.HasIndex(v => v.Sku)
            .IsUnique();

        builder.Property(v => v.Name)
            .HasMaxLength(ProductVariant.NameMaxLength)
            .IsRequired();

        builder.Property(v => v.Price)
            .HasPrecision(12, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(v => v.Currency)
            .HasMaxLength(ProductVariant.CurrencyLength)
            .HasDefaultValue(ProductVariant.DefaultCurrency)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasDefaultValue(ProductVariantStatus.Draft);

        builder.ConfigureAuditColumns();

        builder.HasMany(v => v.Attributes)
            .WithOne()
            .HasForeignKey(a => a.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.Attributes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
