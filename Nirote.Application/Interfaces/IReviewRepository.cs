using Nirote.Domain.Entities;

namespace Nirote.Application.Interfaces;

public interface IReviewRepository
{
    Task<IEnumerable<Review>> GetByProductAsync(int productId, bool approvedOnly);
    Task<IEnumerable<Review>> GetAllAsync();
    Task<IEnumerable<Review>> GetFeaturedAsync(int count);
    Task CreateAsync(Review review);
    Task ApproveAsync(int id);
    Task DeleteAsync(int id);
}
