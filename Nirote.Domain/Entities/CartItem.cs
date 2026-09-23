using System.Text.Json.Serialization;

namespace Nirote.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Populated by sp_Carts_GetWithItems join
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    [JsonPropertyName("imageUrl")]
    public string? DefaultImage { get; set; }
    public int StockQuantity { get; set; }
}
