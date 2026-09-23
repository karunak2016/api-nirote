namespace Nirote.Application.DTOs.Product;

public class CreateProductDto
{
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
    public int DeliveryDays { get; set; } = 5;
    public int CategoryId { get; set; }
    public string? VideoUrl { get; set; }
}
