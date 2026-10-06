using CommerceHub.ProductCatalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(c => c.Name)
            .HasMaxLength(Category.NameMaxLength)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasMaxLength(Category.SlugMaxLength)
            .IsRequired();
        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Status)
            .HasDefaultValue(CategoryStatus.Active);

        // Adjacency list: deleting a category that still has children is rejected.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
