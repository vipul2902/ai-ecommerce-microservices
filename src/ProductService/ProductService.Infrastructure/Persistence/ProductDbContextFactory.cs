using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductService.Infrastructure.Persistence;

/// <summary>
/// Used only by EF Core design-time tools (`dotnet ef migrations add/update`), so that
/// generating migrations doesn't require running the full API host or a reachable database.
/// Not used at application runtime — see DependencyInjection.AddInfrastructure for that.
/// </summary>
public class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=ProductServiceDb;Trusted_Connection=True;TrustServerCertificate=True;");

        return new ProductDbContext(optionsBuilder.Options);
    }
}
