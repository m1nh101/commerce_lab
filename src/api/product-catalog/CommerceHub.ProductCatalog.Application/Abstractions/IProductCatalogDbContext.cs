using CommerceHub.ProductCatalog.Domain.Categories;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Application.Abstractions;

public interface IProductCatalogDbContext
{
    DbSet<Category> Categories { get; }

    DbSet<ProductAttribute> Attributes { get; }

    DbSet<ProductVariantAttribute> ProductVariantAttributes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
