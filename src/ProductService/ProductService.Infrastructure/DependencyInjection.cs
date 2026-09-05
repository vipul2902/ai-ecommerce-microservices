using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Application.Abstractions;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The connection string is read lazily (when the DbContext is first resolved,
        // i.e. per-request) rather than eagerly here, so that test hosts (WebApplicationFactory)
        // which supply configuration after this method runs, and which replace this DbContext
        // registration entirely, are not affected by this check.
        services.AddDbContext<ProductDbContext>((_, options) =>
        {
            var connectionString = configuration.GetConnectionString("ProductServiceDb");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ProductServiceDb' is not configured. Set it via user-secrets or the " +
                    "ConnectionStrings__ProductServiceDb environment variable.");
            }

            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
