using System.Net;
using System.Net.Http.Json;
using ProductService.Application.Dtos;
using Xunit;

namespace ProductService.IntegrationTests;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static CreateProductRequest NewCreateRequest(string? sku = null, string category = "Electronics") => new()
    {
        Name = "Wireless Mouse",
        Description = "A mouse, but wireless.",
        Sku = sku ?? $"SKU-{Guid.NewGuid():N}",
        Category = category,
        Price = 19.99m,
        StockQuantity = 100
    };

    [Fact]
    public async Task Create_WithValidData_Returns201AndLocationHeader()
    {
        var request = NewCreateRequest();

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(request.Sku.ToUpperInvariant(), product!.Sku);
    }

    [Fact]
    public async Task Create_WithDuplicateSku_Returns409()
    {
        var request = NewCreateRequest();

        var first = await _client.PostAsJsonAsync("/api/products", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/products", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingName_Returns400()
    {
        var request = NewCreateRequest();
        request.Name = "";

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNegativePrice_Returns400()
    {
        var request = NewCreateRequest();
        request.Price = -5m;

        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForKnownId_ReturnsProduct()
    {
        var created = await _client.PostAsJsonAsync("/api/products", NewCreateRequest());
        var createdProduct = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var response = await _client.GetAsync($"/api/products/{createdProduct!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(createdProduct.Sku, product!.Sku);
    }

    [Fact]
    public async Task List_FiltersByCategory()
    {
        var category = $"Category-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products", NewCreateRequest(category: category));
        await _client.PostAsJsonAsync("/api/products", NewCreateRequest(category: "SomethingElse"));

        var response = await _client.GetAsync($"/api/products?category={category}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.All(products!, p => Assert.Equal(category, p.Category));
    }

    [Fact]
    public async Task Update_ForUnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", new UpdateProductRequest
        {
            Name = "Name",
            Category = "Electronics",
            Price = 9.99m,
            StockQuantity = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ForKnownId_UpdatesFieldsAndIsReflectedOnNextGet()
    {
        var created = await _client.PostAsJsonAsync("/api/products", NewCreateRequest());
        var createdProduct = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{createdProduct!.Id}", new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Category = "UpdatedCategory",
            Price = 49.99m,
            StockQuantity = 7
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        var product = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Equal("Updated Name", product!.Name);
        Assert.Equal("UpdatedCategory", product.Category);
        Assert.Equal(49.99m, product.Price);
        Assert.Equal(7, product.StockQuantity);
        Assert.Equal(createdProduct.Sku, product.Sku); // SKU unchanged by update
    }

    [Fact]
    public async Task Delete_ForUnknownId_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ForKnownId_Returns204AndSubsequentGetReturns404()
    {
        var created = await _client.PostAsJsonAsync("/api/products", NewCreateRequest());
        var createdProduct = await created.Content.ReadFromJsonAsync<ProductResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/products/{createdProduct!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
