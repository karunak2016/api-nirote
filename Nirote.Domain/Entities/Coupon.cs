namespace Nirote.Domain.Entities;

public class Coupon
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty; // "Percentage" | "Fixed"
    public decimal DiscountValue { get; set; }
    public decimal? MinCartAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public string? FestivalName { get; set; }
    public string? BankName { get; set; }
    public DateTime CreatedAt { get; set; }
}
