using ProductService.Application.Common;
using ProductService.Application.Dtos;

namespace ProductService.Application.Products;

public interface IProductCatalogService
{
    Task<ServiceResult<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<ProductResponse>>> ListAsync(string? category, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProductResponse>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
