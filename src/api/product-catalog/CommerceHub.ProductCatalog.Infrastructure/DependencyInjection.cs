using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Infrastructure.Database;
using CommerceHub.ProductCatalog.Infrastructure.Database.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.ProductCatalog.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ProductCatalog";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ProductCatalogDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IProductCatalogDbContext, ProductCatalogDbContext>();

        return services;
    }
}
