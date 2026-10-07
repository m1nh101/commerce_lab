using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CommerceHub.Cqrs;

public static class ServiceCollectionExtensions
{
    private static readonly Type[] HandlerInterfaces = [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    /// <summary>
    /// Registers every concrete command/query handler in <paramref name="assembly"/> (internal types included) as scoped.
    /// </summary>
    public static IServiceCollection AddCqrsHandlers(this IServiceCollection services, Assembly assembly)
    {
        var registrations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && HandlerInterfaces.Contains(i.GetGenericTypeDefinition()))
                .Select(i => (Service: i, Implementation: t)));

        foreach (var (service, implementation) in registrations)
        {
            services.AddScoped(service, implementation);
        }

        return services;
    }
}
