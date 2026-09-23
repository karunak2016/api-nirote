using Nirote.Application.DTOs.Product;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Campaign = Nirote.Domain.Entities.Campaign;
using Microsoft.Extensions.Logging;

namespace Nirote.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly ICampaignRepository _campaigns;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repo, ICampaignRepository campaigns, ILogger<ProductService> logger)
    {
        _repo = repo;
        _campaigns = campaigns;
        _logger = logger;
    }

    public async Task<PagedResult<ProductListDto>> GetAllAsync(int page, int pageSize, int? categoryId,
        decimal? minPrice, decimal? maxPrice, string? color, string? fabric, string? sortBy, string? state = null,
        decimal? minRating = null, bool? featured = null)
    {
        try
        {
            _logger.LogInformation("GetAll products page={Page} pageSize={PageSize}", page, pageSize);
            var (items, total) = await _repo.GetFilteredAsync(page, pageSize, categoryId, minPrice, maxPrice, color, fabric, sortBy, state, minRating, featured);
            var activeCampaigns = await _campaigns.GetActiveCampaignsAsync();
            return new PagedResult<ProductListDto>
            {
                Items = items.Select(p => MapToList(p, activeCampaigns)).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll products failed");
            throw;
        }
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("GetById product {Id}", id);
            var (product, images) = await _repo.GetByIdWithImagesAsync(id);
            if (product == null) return null;
            var activeCampaigns = await _campaigns.GetActiveCampaignsAsync();
            return MapToDto(product, images, activeCampaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetById product {Id} failed", id);
            throw;
        }
    }

    public async Task<PagedResult<ProductListDto>> SearchAsync(string query, int page, int pageSize)
    {
        try
        {
            _logger.LogInformation("Search products query={Query}", query);
            var (items, total) = await _repo.SearchAsync(query, page, pageSize);
            var activeCampaigns = await _campaigns.GetActiveCampaignsAsync();
            return new PagedResult<ProductListDto>
            {
                Items = items.Select(p => MapToList(p, activeCampaigns)).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search products failed for query={Query}", query);
            throw;
        }
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        try
        {
            _logger.LogInformation("Creating product: {Name}", dto.Name);
            var product = new Product
            {
                Name = dto.Name, Description = dto.Description, Price = dto.Price,
                DiscountedPrice = dto.DiscountedPrice, StockQuantity = dto.StockQuantity,
                Fabric = dto.Fabric, Color = dto.Color, State = dto.State,
                HasBlousePiece = dto.HasBlousePiece,
                CareInstructions = dto.CareInstructions, DeliveryDays = dto.DeliveryDays,
                CategoryId = dto.CategoryId, VideoUrl = dto.VideoUrl
            };
            product.Id = await _repo.CreateAsync(product);
            _logger.LogInformation("Product created with Id {Id}", product.Id);
            return MapToDto(product, Enumerable.Empty<ProductImage>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create product failed: {Name}", dto.Name);
            throw;
        }
    }

    public async Task<ProductDto> UpdateAsync(int id, CreateProductDto dto)
    {
        try
        {
            _logger.LogInformation("Updating product {Id}", id);
            var (existing, images) = await _repo.GetByIdWithImagesAsync(id);
            if (existing == null) throw new KeyNotFoundException($"Product {id} not found.");

            existing.Name = dto.Name; existing.Description = dto.Description;
            existing.Price = dto.Price; existing.DiscountedPrice = dto.DiscountedPrice;
            existing.StockQuantity = dto.StockQuantity; existing.Fabric = dto.Fabric;
            existing.Color = dto.Color; existing.State = dto.State;
            existing.HasBlousePiece = dto.HasBlousePiece;
            existing.CareInstructions = dto.CareInstructions; existing.DeliveryDays = dto.DeliveryDays;
            existing.CategoryId = dto.CategoryId; existing.VideoUrl = dto.VideoUrl;

            await _repo.UpdateAsync(existing);
            _logger.LogInformation("Product {Id} updated", id);
            return MapToDto(existing, images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update product {Id} failed", id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("Deleting product {Id}", id);
            await _repo.DeactivateAsync(id);
            _logger.LogInformation("Product {Id} deactivated", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete product {Id} failed", id);
            throw;
        }
    }

    public async Task<int> AddImageAsync(int productId, string imageUrl, bool isDefault)
    {
        try
        {
            _logger.LogInformation("Adding image to product {ProductId}", productId);
            var imageId = await _repo.AddImageAsync(productId, imageUrl, isDefault, 0);
            _logger.LogInformation("Image {ImageId} added to product {ProductId}", imageId, productId);
            return imageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add image to product {ProductId} failed", productId);
            throw;
        }
    }

    public async Task RemoveImageAsync(int productId, int imageId)
    {
        try
        {
            _logger.LogInformation("Removing image {ImageId} from product {ProductId}", imageId, productId);
            await _repo.RemoveImageAsync(imageId);
            _logger.LogInformation("Image {ImageId} removed", imageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove image {ImageId} failed", imageId);
            throw;
        }
    }

    private static decimal? ApplyCampaign(Product p, IList<Campaign> campaigns)
    {
        var best = campaigns
            .Where(c => c.CategoryId == null || c.CategoryId == p.CategoryId)
            .Select(c => c.DiscountType == "Percentage"
                ? p.Price * c.DiscountValue / 100m
                : c.DiscountValue)
            .DefaultIfEmpty(0m)
            .Max();
        return best > 0 ? Math.Round(p.Price - best, 2) : null;
    }

    private static decimal? EffectiveDiscountedPrice(Product p, IList<Campaign> campaigns)
    {
        var campaignPrice = ApplyCampaign(p, campaigns);
        if (campaignPrice == null) return p.DiscountedPrice;
        if (p.DiscountedPrice == null) return campaignPrice;
        return Math.Min(campaignPrice.Value, p.DiscountedPrice.Value);
    }

    private static ProductListDto MapToList(Product p, IList<Campaign>? campaigns = null) => new()
    {
        Id = p.Id, Name = p.Name, Price = p.Price,
        DiscountedPrice = campaigns != null ? EffectiveDiscountedPrice(p, campaigns) : p.DiscountedPrice,
        Fabric = p.Fabric ?? string.Empty, Color = p.Color ?? string.Empty, State = p.State,
        CategoryName = p.CategoryName ?? string.Empty,
        DefaultImageUrl = p.DefaultImage,
        StockQuantity = p.StockQuantity,
        IsActive = p.IsActive,
        AverageRating = p.AverageRating,
        ReviewCount = p.ReviewCount
    };

    private static ProductDto MapToDto(Product p, IEnumerable<ProductImage> images, IList<Campaign>? campaigns = null)
    {
        var imgList = images.OrderBy(i => i.DisplayOrder).ToList();
        var imageDtos = imgList.Select(i => new ProductImageDto
        {
            Id = i.Id,
            Url = i.ImageUrl,
            IsDefault = i.IsDefault
        }).ToList();
        return new ProductDto
        {
            Id = p.Id, Name = p.Name, Description = p.Description ?? string.Empty,
            Price = p.Price,
            DiscountedPrice = campaigns != null ? EffectiveDiscountedPrice(p, campaigns) : p.DiscountedPrice,
            StockQuantity = p.StockQuantity, Fabric = p.Fabric ?? string.Empty,
            Color = p.Color ?? string.Empty, State = p.State, HasBlousePiece = p.HasBlousePiece,
            CareInstructions = p.CareInstructions, DeliveryDays = p.DeliveryDays,
            CategoryId = p.CategoryId, CategoryName = p.CategoryName ?? string.Empty,
            IsActive = p.IsActive,
            Images = imageDtos,
            ImageUrls = imageDtos.Select(i => i.Url).ToList(),
            DefaultImageUrl = imageDtos.FirstOrDefault(i => i.IsDefault)?.Url,
            VideoUrl = p.VideoUrl,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount
        };
    }
}

public interface IProductRepository
{
    Task<(List<Product> Items, int Total)> GetFilteredAsync(int page, int pageSize, int? categoryId,
        decimal? minPrice, decimal? maxPrice, string? color, string? fabric, string? sortBy, string? state = null,
        decimal? minRating = null, bool? featured = null);
    Task<(List<Product> Items, int Total)> SearchAsync(string query, int page, int pageSize);
    Task<(Product? Product, IEnumerable<ProductImage> Images)> GetByIdWithImagesAsync(int id);
    Task<int> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeactivateAsync(int productId);
    Task<int> AddImageAsync(int productId, string imageUrl, bool isDefault, int displayOrder);
    Task RemoveImageAsync(int imageId);
}
