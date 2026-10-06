using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.Url)
            .HasMaxLength(ProductImage.UrlMaxLength)
            .IsRequired();

        builder.Property(i => i.SortOrder)
            .HasDefaultValue(0)
            .IsRequired();

        // Optional link to a variant; when null the image applies to the whole product.
        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(i => i.VariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_product_images_product_variants_variant_id");
    }
}
