using System.Security.Claims;
using Nirote.Application.DTOs.Cart;
using Nirote.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer,Admin")]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cart, ILogger<CartController> logger)
    {
        _cart = cart;
        _logger = logger;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            _logger.LogInformation("Get cart for user {UserId}", UserId);
            return Ok(await _cart.GetCartAsync(UserId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get cart failed for user {UserId}", UserId);
            throw;
        }
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartDto dto)
    {
        try
        {
            _logger.LogInformation("Add item product {ProductId} to cart for user {UserId}", dto.ProductId, UserId);
            return Ok(await _cart.AddItemAsync(UserId, dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add item to cart failed for user {UserId}", UserId);
            throw;
        }
    }

    [HttpPost("items/update/{id:int}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] int quantity)
    {
        try
        {
            _logger.LogInformation("Update cart item {Id} to qty {Qty} for user {UserId}", id, quantity, UserId);
            return Ok(await _cart.UpdateItemAsync(UserId, id, quantity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update cart item {Id} failed for user {UserId}", id, UserId);
            throw;
        }
    }

    [HttpPost("items/remove/{id:int}")]
    public async Task<IActionResult> RemoveItem(int id)
    {
        try
        {
            _logger.LogInformation("Remove cart item {Id} for user {UserId}", id, UserId);
            return Ok(await _cart.RemoveItemAsync(UserId, id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove cart item {Id} failed for user {UserId}", id, UserId);
            throw;
        }
    }

    [HttpPost("clear")]
    public async Task<IActionResult> Clear()
    {
        try
        {
            _logger.LogInformation("Clear cart for user {UserId}", UserId);
            await _cart.ClearCartAsync(UserId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear cart failed for user {UserId}", UserId);
            throw;
        }
    }
}
