using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Images;
using CommerceHub.ProductCatalog.Infrastructure.Database;
using CommerceHub.ProductCatalog.Infrastructure.Database.Interceptors;
using CommerceHub.ProductCatalog.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.AddProductImageStorage(configuration);

        return services;
    }

    private static void AddProductImageStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ImageUploadOptions>()
            .Bind(configuration.GetSection(ImageUploadOptions.SectionName));

        services.AddOptions<ProductImageStorageOptions>()
            .Bind(configuration.GetSection(ProductImageStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ProductImageStorageOptions>>().Value;
            var config = new AmazonS3Config { ForcePathStyle = settings.ForcePathStyle };
            if (string.IsNullOrWhiteSpace(settings.ServiceUrl))
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);
            }
            else
            {
                config.ServiceURL = settings.ServiceUrl;
                config.AuthenticationRegion = settings.Region;
            }

            return string.IsNullOrWhiteSpace(settings.AccessKey)
                ? new AmazonS3Client(config)
                : new AmazonS3Client(new BasicAWSCredentials(settings.AccessKey, settings.SecretKey), config);
        });

        services.AddSingleton<IProductImageStorage, S3ProductImageStorage>();
    }
}
