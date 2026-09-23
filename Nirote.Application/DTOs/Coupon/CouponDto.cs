namespace Nirote.Application.DTOs.Coupon;

public class CouponDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinCartAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public string? FestivalName { get; set; }
    public string? BankName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCouponDto
{
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal? MinCartAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public string? FestivalName { get; set; }
    public string? BankName { get; set; }
}

public class ValidateCouponDto
{
    public string Code { get; set; } = string.Empty;
    public decimal CartTotal { get; set; }
}

public class UpdateCouponDto : CreateCouponDto
{
    public bool IsActive { get; set; } = true;
}

public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? CouponId { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Description { get; set; }
}
