using CommerceHub.ProductCatalog.Domain.Categories;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        builder.HasKey(pc => new { pc.ProductId, pc.CategoryId });

        // Category is a separate aggregate: keep the FK, but no navigation property.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
