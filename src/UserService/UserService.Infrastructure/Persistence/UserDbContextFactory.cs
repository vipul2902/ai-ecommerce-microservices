using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserService.Infrastructure.Persistence;

/// <summary>
/// Used only by EF Core design-time tools (`dotnet ef migrations add/update`), so that
/// generating migrations doesn't require running the full API host or a reachable database.
/// Not used at application runtime — see DependencyInjection.AddInfrastructure for that.
/// </summary>
public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=UserServiceDb;Trusted_Connection=True;TrustServerCertificate=True;");

        return new UserDbContext(optionsBuilder.Options);
    }
}
