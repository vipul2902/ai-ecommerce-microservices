using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Users;

namespace UserService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserAccountService, UserAccountService>();
        return services;
    }
}
