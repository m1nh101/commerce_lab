using CommerceHub.ProductCatalog.Application.Attributes;
using CommerceHub.ProductCatalog.Application.Categories;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.ProductCatalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAttributeService, AttributeService>();

        return services;
    }
}
