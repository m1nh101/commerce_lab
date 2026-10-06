using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasDefaultValueSql("uuidv7()");

        builder.Property(p => p.Name)
            .HasMaxLength(Product.NameMaxLength)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(Product.SlugMaxLength)
            .IsRequired();
        builder.HasIndex(p => p.Slug)
            .IsUnique();

        builder.Property(p => p.Description)
            .HasColumnType("text");

        builder.Property(p => p.Status)
            .HasDefaultValue(ProductStatus.Draft);

        builder.ConfigureAuditColumns();

        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Categories)
            .WithOne()
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Images).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Categories).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
