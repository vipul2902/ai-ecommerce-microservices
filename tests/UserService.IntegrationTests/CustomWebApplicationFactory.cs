using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Infrastructure.Persistence;

namespace UserService.IntegrationTests;

/// <summary>
/// Boots the real API host, but swaps the SQL Server-backed DbContext for an EF Core
/// InMemory one and supplies test-only Jwt/connection-string configuration, so the
/// integration tests don't need a reachable SQL Server instance.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"UserServiceTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UserServiceDb"] = "Server=(local);Database=UserServiceTests;Trusted_Connection=True;",
                ["Jwt:Issuer"] = "UserService.IntegrationTests",
                ["Jwt:Audience"] = "UserService.IntegrationTests.Audience",
                ["Jwt:Key"] = "integration-test-signing-key-at-least-32-chars-long",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbContextOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<UserDbContext>));
            if (dbContextOptionsDescriptor is not null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase(DatabaseName));
        });
    }
}
