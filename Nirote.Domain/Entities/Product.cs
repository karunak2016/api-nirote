namespace Nirote.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Fabric { get; set; }
    public string? Color { get; set; }
    public string? Occasion { get; set; }
    public string? State { get; set; }
    public bool HasBlousePiece { get; set; }
    public string? CareInstructions { get; set; }
    public int DeliveryDays { get; set; } = 7;
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public string? VideoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Populated by JOIN queries (not stored columns)
    public string? CategoryName { get; set; }
    public string? DefaultImage { get; set; }
    public decimal? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
