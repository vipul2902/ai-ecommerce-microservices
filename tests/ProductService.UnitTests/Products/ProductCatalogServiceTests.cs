using Moq;
using ProductService.Application.Abstractions;
using ProductService.Application.Common;
using ProductService.Application.Dtos;
using ProductService.Application.Products;
using ProductService.Domain.Entities;
using Xunit;

namespace ProductService.UnitTests.Products;

public class ProductCatalogServiceTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly ProductCatalogService _sut;

    public ProductCatalogServiceTests()
    {
        _sut = new ProductCatalogService(_productRepository.Object);
    }

    private static CreateProductRequest NewCreateRequest(string? sku = null) => new()
    {
        Name = "Wireless Mouse",
        Description = "A mouse, but wireless.",
        Sku = sku ?? "SKU-001",
        Category = "Electronics",
        Price = 19.99m,
        StockQuantity = 100
    };

    [Fact]
    public async Task CreateAsync_WithNewSku_CreatesProductAndNormalizesSku()
    {
        _productRepository.Setup(r => r.SkuExistsAsync("SKU-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Product? captured = null;
        _productRepository.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(NewCreateRequest(sku: "sku-001"));

        Assert.True(result.Succeeded);
        Assert.Equal("SKU-001", result.Value!.Sku);
        Assert.NotNull(captured);
        Assert.Equal("SKU-001", captured!.Sku);
        _productRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSku_ReturnsConflictAndDoesNotCreateProduct()
    {
        _productRepository.Setup(r => r.SkuExistsAsync("SKU-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.CreateAsync(NewCreateRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Conflict, result.Error!.Type);
        _productRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _productRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ForUnknownId_ReturnsNotFound()
    {
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task GetByIdAsync_ForKnownId_ReturnsMappedProduct()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Wireless Mouse",
            Sku = "SKU-001",
            Category = "Electronics",
            Price = 19.99m,
            StockQuantity = 100
        };
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(product.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(product.Sku, result.Value!.Sku);
        Assert.Equal(product.Price, result.Value.Price);
    }

    [Fact]
    public async Task ListAsync_PassesCategoryFilterThroughToRepository()
    {
        _productRepository.Setup(r => r.ListAsync("Electronics", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>
            {
                new() { Id = Guid.NewGuid(), Name = "Mouse", Sku = "SKU-001", Category = "Electronics" }
            });

        var result = await _sut.ListAsync("Electronics");

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        _productRepository.Verify(r => r.ListAsync("Electronics", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ForUnknownId_ReturnsNotFound()
    {
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateProductRequest
        {
            Name = "New Name",
            Category = "Electronics",
            Price = 9.99m,
            StockQuantity = 5
        });

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsButLeavesSkuUnchanged()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Sku = "SKU-001",
            Category = "Old Category",
            Price = 5.00m,
            StockQuantity = 1
        };
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.UpdateAsync(product.Id, new UpdateProductRequest
        {
            Name = "New Name",
            Description = "New description",
            Category = "New Category",
            Price = 25.00m,
            StockQuantity = 50
        });

        Assert.True(result.Succeeded);
        Assert.Equal("New Name", product.Name);
        Assert.Equal("New Category", product.Category);
        Assert.Equal(25.00m, product.Price);
        Assert.Equal(50, product.StockQuantity);
        Assert.Equal("SKU-001", product.Sku); // SKU is immutable via update
        _productRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ForUnknownId_ReturnsNotFound()
    {
        _productRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
        _productRepository.Verify(r => r.Remove(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ForKnownId_RemovesAndSaves()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "Mouse", Sku = "SKU-001", Category = "Electronics" };
        _productRepository.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.DeleteAsync(product.Id);

        Assert.True(result.Succeeded);
        _productRepository.Verify(r => r.Remove(product), Times.Once);
        _productRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
