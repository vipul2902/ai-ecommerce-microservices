using ProductService.Domain.Entities;

namespace ProductService.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>Lists products, optionally filtered by exact category match (case-insensitive).</summary>
    Task<IReadOnlyList<Product>> ListAsync(string? category, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    void Remove(Product product);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
