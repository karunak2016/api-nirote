using Nirote.Application.DTOs.Cart;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Nirote.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _repo;
    private readonly ILogger<CartService> _logger;

    public CartService(ICartRepository repo, ILogger<CartService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Get cart for user {UserId}", userId);
            var (header, items) = await _repo.GetWithItemsAsync(userId);
            return MapToDto(header, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get cart failed for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CartDto> AddItemAsync(int userId, AddToCartDto dto)
    {
        try
        {
            _logger.LogInformation("Add product {ProductId} to cart for user {UserId}", dto.ProductId, userId);
            await _repo.AddOrUpdateItemAsync(userId, dto.ProductId, dto.Quantity);
            return await GetCartAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add item to cart failed for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CartDto> UpdateItemAsync(int userId, int cartItemId, int quantity)
    {
        try
        {
            _logger.LogInformation("Update cart item {CartItemId} qty={Qty} for user {UserId}", cartItemId, quantity, userId);
            var (success, error) = await _repo.UpdateQuantityAsync(cartItemId, userId, quantity);
            if (!success) throw new InvalidOperationException(error ?? "Update failed.");
            return await GetCartAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update cart item {CartItemId} failed for user {UserId}", cartItemId, userId);
            throw;
        }
    }

    public async Task<CartDto> RemoveItemAsync(int userId, int cartItemId)
    {
        try
        {
            _logger.LogInformation("Remove cart item {CartItemId} for user {UserId}", cartItemId, userId);
            await _repo.RemoveItemAsync(cartItemId, userId);
            return await GetCartAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove cart item {CartItemId} failed for user {UserId}", cartItemId, userId);
            throw;
        }
    }

    public async Task ClearCartAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Clear cart for user {UserId}", userId);
            await _repo.ClearAsync(userId);
            _logger.LogInformation("Cart cleared for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear cart failed for user {UserId}", userId);
            throw;
        }
    }

    private static CartDto MapToDto(Cart? header, IEnumerable<CartItem> items)
    {
        var list = items.Select(i => new CartItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName ?? string.Empty,
            ImageUrl = i.DefaultImage,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList();

        return new CartDto { Id = header?.Id ?? 0, Items = list };
    }
}

public interface ICartRepository
{
    Task<int> GetOrCreateAsync(int userId);
    Task<(Cart? Header, IEnumerable<CartItem> Items)> GetWithItemsAsync(int userId);
    Task ClearAsync(int userId);
    Task<(int CartItemId, int NewQuantity)> AddOrUpdateItemAsync(int userId, int productId, int quantity);
    Task<(bool Success, string? Error)> UpdateQuantityAsync(int cartItemId, int userId, int quantity);
    Task RemoveItemAsync(int cartItemId, int userId);
    Task<int> GetItemCountAsync(int userId);
}
