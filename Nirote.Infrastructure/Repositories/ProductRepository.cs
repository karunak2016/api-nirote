using System.Data;
using Dapper;
using Nirote.Application.Services;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _db;

    public ProductRepository(IDbConnectionFactory db) => _db = db;

    public async Task<(List<Product> Items, int Total)> GetFilteredAsync(int page, int pageSize, int? categoryId,
        decimal? minPrice, decimal? maxPrice, string? color, string? fabric, string? sortBy, string? state = null,
        decimal? minRating = null, bool? featured = null)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@CategoryId", categoryId);
        p.Add("@MinPrice", minPrice);
        p.Add("@MaxPrice", maxPrice);
        p.Add("@Color", color);
        p.Add("@Fabric", fabric);
        p.Add("@State", state);
        p.Add("@MinRating", minRating);
        p.Add("@IsFeatured", featured.HasValue ? (object)(featured.Value ? 1 : 0) : null);
        p.Add("@SortBy", sortBy ?? "newest");
        p.Add("@Page", page);
        p.Add("@PageSize", pageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = await conn.QueryAsync<Product>("sp_Products_GetAll", p, commandType: CommandType.StoredProcedure);
        return (items.ToList(), p.Get<int>("@TotalCount"));
    }

    public async Task<(List<Product> Items, int Total)> SearchAsync(string query, int page, int pageSize)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@SearchTerm", query);
        p.Add("@SortBy", "newest");
        p.Add("@Page", page);
        p.Add("@PageSize", pageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = await conn.QueryAsync<Product>("sp_Products_GetAll", p, commandType: CommandType.StoredProcedure);
        return (items.ToList(), p.Get<int>("@TotalCount"));
    }

    public async Task<(Product? Product, IEnumerable<ProductImage> Images)> GetByIdWithImagesAsync(int id)
    {
        using var conn = _db.Create();
        using var multi = await conn.QueryMultipleAsync(
            "sp_Products_GetById",
            new { ProductId = id },
            commandType: CommandType.StoredProcedure);

        var product = await multi.ReadSingleOrDefaultAsync<Product>();
        var images = await multi.ReadAsync<ProductImage>();
        return (product, images);
    }

    public async Task<int> CreateAsync(Product product)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@Name", product.Name);
        p.Add("@Description", product.Description);
        p.Add("@Price", product.Price);
        p.Add("@DiscountedPrice", product.DiscountedPrice);
        p.Add("@StockQuantity", product.StockQuantity);
        p.Add("@Fabric", product.Fabric);
        p.Add("@Color", product.Color);
        p.Add("@Occasion", product.Occasion);
        p.Add("@State", product.State);
        p.Add("@HasBlousePiece", product.HasBlousePiece);
        p.Add("@CareInstructions", product.CareInstructions);
        p.Add("@DeliveryDays", product.DeliveryDays);
        p.Add("@CategoryId", product.CategoryId);
        p.Add("@VideoUrl", product.VideoUrl);
        p.Add("@NewProductId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Products_Create", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewProductId");
    }

    public async Task UpdateAsync(Product product)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Products_Update", new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.DiscountedPrice,
            product.StockQuantity,
            product.Fabric,
            product.Color,
            product.Occasion,
            product.State,
            product.HasBlousePiece,
            product.CareInstructions,
            product.DeliveryDays,
            product.CategoryId,
            product.VideoUrl,
            ProductId = product.Id
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task DeactivateAsync(int productId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Products_Deactivate",
            new { ProductId = productId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> AddImageAsync(int productId, string imageUrl, bool isDefault, int displayOrder)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@ProductId", productId);
        p.Add("@ImageUrl", imageUrl);
        p.Add("@IsDefault", isDefault);
        p.Add("@DisplayOrder", displayOrder);
        p.Add("@NewImageId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_ProductImages_Add", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewImageId");
    }

    public async Task RemoveImageAsync(int imageId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_ProductImages_Delete",
            new { ImageId = imageId },
            commandType: CommandType.StoredProcedure);
    }
}
