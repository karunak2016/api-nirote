using Nirote.Application.DTOs.Product;

namespace Nirote.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductListDto>> GetAllAsync(int page, int pageSize, int? categoryId,
        decimal? minPrice, decimal? maxPrice, string? color, string? fabric, string? sortBy, string? state = null,
        decimal? minRating = null, bool? featured = null);
    Task<ProductDto?> GetByIdAsync(int id);
    Task<PagedResult<ProductListDto>> SearchAsync(string query, int page, int pageSize);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(int id, CreateProductDto dto);
    Task DeleteAsync(int id);
    Task<int> AddImageAsync(int productId, string imageUrl, bool isDefault);
    Task RemoveImageAsync(int productId, int imageId);
}
