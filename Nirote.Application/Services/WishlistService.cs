using Nirote.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Nirote.Application.Services;

public record WishlistItemDto(int ProductId, string ProductName, string? ImageUrl, decimal Price);

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _repo;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(IWishlistRepository repo, ILogger<WishlistService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<WishlistItemDto>> GetWishlistAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Get wishlist for user {UserId}", userId);
            return await _repo.GetByUserAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get wishlist failed for user {UserId}", userId);
            throw;
        }
    }

    public async Task AddToWishlistAsync(int userId, int productId)
    {
        try
        {
            _logger.LogInformation("Add product {ProductId} to wishlist for user {UserId}", productId, userId);
            await _repo.AddAsync(userId, productId);
            _logger.LogInformation("Product {ProductId} added to wishlist for user {UserId}", productId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add to wishlist failed for user {UserId} product {ProductId}", userId, productId);
            throw;
        }
    }

    public async Task RemoveFromWishlistAsync(int userId, int productId)
    {
        try
        {
            _logger.LogInformation("Remove product {ProductId} from wishlist for user {UserId}", productId, userId);
            await _repo.RemoveAsync(userId, productId);
            _logger.LogInformation("Product {ProductId} removed from wishlist for user {UserId}", productId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove from wishlist failed for user {UserId} product {ProductId}", userId, productId);
            throw;
        }
    }

    public async Task<bool> CheckAsync(int userId, int productId)
    {
        try
        {
            _logger.LogInformation("Check wishlist user {UserId} product {ProductId}", userId, productId);
            return await _repo.CheckAsync(userId, productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check wishlist failed for user {UserId} product {ProductId}", userId, productId);
            throw;
        }
    }
}

public interface IWishlistRepository
{
    Task AddAsync(int userId, int productId);
    Task RemoveAsync(int userId, int productId);
    Task<IEnumerable<WishlistItemDto>> GetByUserAsync(int userId);
    Task<bool> CheckAsync(int userId, int productId);
}
