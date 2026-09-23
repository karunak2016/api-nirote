using Nirote.Application.DTOs.Coupon;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Nirote.Application.Services;

public class CouponService(ICouponRepository repo, ILogger<CouponService> logger) : ICouponService
{
    public async Task<List<CouponDto>> GetAllAsync()
    {
        var coupons = await repo.GetAllAsync();
        return coupons.Select(Map).ToList();
    }

    public async Task<List<CouponDto>> GetActiveAutoOffersAsync()
    {
        var offers = await repo.GetActiveAutoOffersAsync();
        return offers.Select(Map).ToList();
    }

    public async Task<List<CouponDto>> GetBankOffersAsync()
    {
        var offers = await repo.GetBankOffersAsync();
        return offers.Select(Map).ToList();
    }

    public async Task<CouponValidationResult> ValidateAsync(string code, decimal cartTotal)
    {
        try
        {
            var coupon = await repo.GetByCodeAsync(code.Trim().ToUpperInvariant());
            if (coupon == null)
                return Fail("Invalid coupon code.");

            if (!coupon.IsActive)
                return Fail("This coupon is no longer active.");

            var now = DateTime.UtcNow;
            if (coupon.StartDate.HasValue && now < coupon.StartDate.Value)
                return Fail("This coupon is not yet valid.");
            if (coupon.EndDate.HasValue && now > coupon.EndDate.Value)
                return Fail("This coupon has expired.");

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                return Fail("This coupon has reached its usage limit.");

            if (coupon.MinCartAmount.HasValue && cartTotal < coupon.MinCartAmount.Value)
                return Fail($"Minimum order value of Rs.{coupon.MinCartAmount:N0} required.");

            var discount = CalculateDiscount(coupon, cartTotal);
            return new CouponValidationResult
            {
                IsValid = true,
                CouponId = coupon.Id,
                DiscountAmount = discount,
                Description = coupon.Description,
                Message = $"{coupon.Description} - Rs.{discount:N0} off applied!"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Coupon validation failed for code {Code}", code);
            throw;
        }
    }

    public async Task<CouponDto> CreateAsync(CreateCouponDto dto)
    {
        var coupon = new Coupon
        {
            Code         = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim().ToUpperInvariant(),
            Description  = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MinCartAmount = dto.MinCartAmount,
            MaxDiscount  = dto.MaxDiscount,
            StartDate    = dto.StartDate,
            EndDate      = dto.EndDate,
            UsageLimit   = dto.UsageLimit,
            FestivalName = dto.FestivalName,
            BankName     = string.IsNullOrWhiteSpace(dto.BankName) ? null : dto.BankName.Trim(),
        };
        coupon.Id = await repo.CreateAsync(coupon);
        logger.LogInformation("Coupon {Code} created with Id {Id}", coupon.Code, coupon.Id);
        return Map(coupon);
    }

    public async Task<CouponDto> UpdateAsync(int id, CreateCouponDto dto, bool isActive)
    {
        var coupon = new Coupon
        {
            Id           = id,
            Code         = string.IsNullOrWhiteSpace(dto.Code) ? null : dto.Code.Trim().ToUpperInvariant(),
            Description  = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MinCartAmount = dto.MinCartAmount,
            MaxDiscount  = dto.MaxDiscount,
            StartDate    = dto.StartDate,
            EndDate      = dto.EndDate,
            UsageLimit   = dto.UsageLimit,
            FestivalName = dto.FestivalName,
            BankName     = string.IsNullOrWhiteSpace(dto.BankName) ? null : dto.BankName.Trim(),
            IsActive     = isActive,
        };
        await repo.UpdateAsync(coupon);
        logger.LogInformation("Coupon {Id} updated", id);
        return Map(coupon);
    }

    public async Task IncrementUsageAsync(int couponId)
    {
        await repo.IncrementUsageAsync(couponId);
    }

    private static decimal CalculateDiscount(Coupon coupon, decimal cartTotal)
    {
        decimal discount = coupon.DiscountType == "Percentage"
            ? cartTotal * coupon.DiscountValue / 100m
            : coupon.DiscountValue;

        if (coupon.MaxDiscount.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscount.Value);

        return Math.Min(Math.Round(discount, 2), cartTotal);
    }

    private static CouponValidationResult Fail(string message) =>
        new() { IsValid = false, Message = message };

    private static CouponDto Map(Coupon c) => new()
    {
        Id = c.Id, Code = c.Code, Description = c.Description,
        DiscountType = c.DiscountType, DiscountValue = c.DiscountValue,
        MinCartAmount = c.MinCartAmount, MaxDiscount = c.MaxDiscount,
        StartDate = c.StartDate, EndDate = c.EndDate,
        IsActive = c.IsActive, UsageLimit = c.UsageLimit,
        UsedCount = c.UsedCount, FestivalName = c.FestivalName, BankName = c.BankName,
        CreatedAt = c.CreatedAt
    };
}
