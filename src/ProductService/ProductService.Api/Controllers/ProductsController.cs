using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Common;
using ProductService.Application.Dtos;
using ProductService.Application.Products;

namespace ProductService.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductCatalogService _productCatalogService;

    public ProductsController(IProductCatalogService productCatalogService)
    {
        _productCatalogService = productCatalogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var result = await _productCatalogService.ListAsync(category, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productCatalogService.GetByIdAsync(id, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result.Error!);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productCatalogService.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapError(result.Error!);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await _productCatalogService.UpdateAsync(id, request, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result.Error!);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _productCatalogService.DeleteAsync(id, cancellationToken);

        return result.Succeeded
            ? NoContent()
            : MapError(result.Error!);
    }

    private ActionResult MapError(ServiceError error) => error.Type switch
    {
        ServiceErrorType.Validation => BadRequest(new { message = error.Message }),
        ServiceErrorType.Conflict => Conflict(new { message = error.Message }),
        ServiceErrorType.NotFound => NotFound(new { message = error.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." })
    };
}
