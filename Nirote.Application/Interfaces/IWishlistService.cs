using Nirote.Application.Services;

namespace Nirote.Application.Interfaces;

public interface IWishlistService
{
    Task<IEnumerable<WishlistItemDto>> GetWishlistAsync(int userId);
    Task AddToWishlistAsync(int userId, int productId);
    Task RemoveFromWishlistAsync(int userId, int productId);
    Task<bool> CheckAsync(int userId, int productId);
}
