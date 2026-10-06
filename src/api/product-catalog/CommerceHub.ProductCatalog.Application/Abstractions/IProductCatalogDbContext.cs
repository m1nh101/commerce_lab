using CommerceHub.ProductCatalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Abstractions;

public interface IProductCatalogDbContext
{
    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
