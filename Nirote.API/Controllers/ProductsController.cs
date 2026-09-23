using Nirote.Application.DTOs.Product;
using Nirote.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService products, ILogger<ProductsController> logger)
    {
        _products = products;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12,
        [FromQuery] int? categoryId = null,
        [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? color = null, [FromQuery] string? fabric = null,
        [FromQuery] string? sortBy = null, [FromQuery] string? state = null,
        [FromQuery] decimal? minRating = null, [FromQuery] bool? featured = null)
    {
        try
        {
            _logger.LogInformation("GetAll products page={Page} pageSize={PageSize}", page, pageSize);
            return Ok(await _products.GetAllAsync(page, pageSize, categoryId, minPrice, maxPrice, color, fabric, sortBy, state, minRating, featured));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll products failed");
            throw;
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            _logger.LogInformation("GetById product {Id}", id);
            var product = await _products.GetByIdAsync(id);
            return product == null ? NotFound() : Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetById product {Id} failed", id);
            throw;
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        try
        {
            _logger.LogInformation("Search products query={Query}", q);
            return Ok(await _products.SearchAsync(q, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search products failed for query={Query}", q);
            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        try
        {
            _logger.LogInformation("Creating product: {Name}", dto.Name);
            var product = await _products.CreateAsync(dto);
            _logger.LogInformation("Product created with Id {Id}", product.Id);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create product failed: {Name}", dto.Name);
            throw;
        }
    }

    [HttpPost("update/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, CreateProductDto dto)
    {
        try
        {
            _logger.LogInformation("Updating product {Id}", id);
            var result = await _products.UpdateAsync(id, dto);
            _logger.LogInformation("Product {Id} updated", id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update product {Id} failed", id);
            throw;
        }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("Deleting product {Id}", id);
            await _products.DeleteAsync(id);
            _logger.LogInformation("Product {Id} deleted", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete product {Id} failed", id);
            throw;
        }
    }

    [HttpPost("{id:int}/images")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddImage(int id, [FromBody] AddImageDto dto)
    {
        try
        {
            _logger.LogInformation("Adding image to product {Id}", id);
            var imageId = await _products.AddImageAsync(id, dto.ImageUrl, dto.IsDefault);
            return Ok(new { imageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add image to product {Id} failed", id);
            throw;
        }
    }

    [HttpPost("{id:int}/images/delete/{imageId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveImage(int id, int imageId)
    {
        try
        {
            _logger.LogInformation("Removing image {ImageId} from product {Id}", imageId, id);
            await _products.RemoveImageAsync(id, imageId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove image {ImageId} from product {Id} failed", imageId, id);
            throw;
        }
    }
}

public class AddImageDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
