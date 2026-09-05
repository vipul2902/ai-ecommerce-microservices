using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Abstractions;
using UserService.Application.Configuration;
using UserService.Infrastructure.Auth;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The connection string is read lazily (when the DbContext is first resolved,
        // i.e. per-request) rather than eagerly here, so that test hosts (WebApplicationFactory)
        // which supply configuration after this method runs, and which replace this DbContext
        // registration entirely, are not affected by this check.
        services.AddDbContext<UserDbContext>((_, options) =>
        {
            var connectionString = configuration.GetConnectionString("UserServiceDb");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'UserServiceDb' is not configured. Set it via user-secrets or the " +
                    "ConnectionStrings__UserServiceDb environment variable.");
            }

            options.UseSqlServer(connectionString);
        });

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
