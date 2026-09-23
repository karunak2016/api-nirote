using Nirote.Application.DTOs.Cart;

namespace Nirote.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);
    Task<CartDto> AddItemAsync(int userId, AddToCartDto dto);
    Task<CartDto> UpdateItemAsync(int userId, int cartItemId, int quantity);
    Task<CartDto> RemoveItemAsync(int userId, int cartItemId);
    Task ClearCartAsync(int userId);
}
