using ProductService.Application.Abstractions;
using ProductService.Application.Common;
using ProductService.Application.Dtos;
using ProductService.Domain.Entities;

namespace ProductService.Application.Products;

public class ProductCatalogService : IProductCatalogService
{
    private readonly IProductRepository _productRepository;

    public ProductCatalogService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ServiceResult<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();

        if (await _productRepository.SkuExistsAsync(normalizedSku, cancellationToken))
        {
            return ServiceResult<ProductResponse>.Failure(
                ServiceErrorType.Conflict,
                "A product with this SKU already exists.");
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Sku = normalizedSku,
            Category = request.Category.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<ServiceResult<ProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        return product is null
            ? ServiceResult<ProductResponse>.Failure(ServiceErrorType.NotFound, "Product not found.")
            : ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<ServiceResult<IReadOnlyList<ProductResponse>>> ListAsync(string? category, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.ListAsync(category, cancellationToken);

        return ServiceResult<IReadOnlyList<ProductResponse>>.Success(
            products.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult<ProductResponse>.Failure(ServiceErrorType.NotFound, "Product not found.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Category = request.Category.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProductResponse>.Success(ToResponse(product));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult<bool>.Failure(ServiceErrorType.NotFound, "Product not found.");
        }

        _productRepository.Remove(product);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static ProductResponse ToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Sku = product.Sku,
        Category = product.Category,
        Price = product.Price,
        StockQuantity = product.StockQuantity,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
