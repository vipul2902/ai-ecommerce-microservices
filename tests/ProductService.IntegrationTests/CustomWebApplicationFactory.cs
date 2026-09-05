using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Infrastructure.Persistence;

namespace ProductService.IntegrationTests;

/// <summary>
/// Boots the real ProductService.Api host, but swaps the SQL Server-backed DbContext for an
/// EF Core InMemory one and supplies a placeholder connection string, so the integration
/// tests don't need a reachable SQL Server instance.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"ProductServiceTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ProductServiceDb"] = "Server=(local);Database=ProductServiceTests;Trusted_Connection=True;"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>));
            if (dbContextOptionsDescriptor is not null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            services.AddDbContext<ProductDbContext>(options => options.UseInMemoryDatabase(DatabaseName));
        });
    }
}
