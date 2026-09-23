using Nirote.Application.DTOs.Coupon;

namespace Nirote.Application.Interfaces;

public interface ICouponService
{
    Task<List<CouponDto>> GetAllAsync();
    Task<List<CouponDto>> GetActiveAutoOffersAsync();
    Task<List<CouponDto>> GetBankOffersAsync();
    Task<CouponValidationResult> ValidateAsync(string code, decimal cartTotal);
    Task<CouponDto> CreateAsync(CreateCouponDto dto);
    Task<CouponDto> UpdateAsync(int id, CreateCouponDto dto, bool isActive);
    Task IncrementUsageAsync(int couponId);
}
