namespace Nirote.Application.DTOs.Product;

public class ProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string? DefaultImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Fabric { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? State { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
