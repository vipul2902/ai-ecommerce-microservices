using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Products;

namespace ProductService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        return services;
    }
}
