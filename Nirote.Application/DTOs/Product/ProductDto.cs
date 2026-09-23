namespace Nirote.Application.DTOs.Product;

public class ProductImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int StockQuantity { get; set; }
    public string Fabric { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Occasion { get; set; }
    public string? State { get; set; }
    public bool HasBlousePiece { get; set; }
    public string? CareInstructions { get; set; }
    public int DeliveryDays { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<string> ImageUrls { get; set; } = new();
    public string? DefaultImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
