using Nirote.Domain.Entities;

namespace Nirote.Application.Interfaces;

public interface ICouponRepository
{
    Task<List<Coupon>> GetAllAsync();
    Task<List<Coupon>> GetActiveAutoOffersAsync();
    Task<List<Coupon>> GetBankOffersAsync();
    Task<Coupon?> GetByCodeAsync(string code);
    Task<int> CreateAsync(Coupon coupon);
    Task UpdateAsync(Coupon coupon);
    Task IncrementUsageAsync(int id);
}
