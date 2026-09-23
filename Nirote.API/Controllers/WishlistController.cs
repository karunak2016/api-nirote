using System.Security.Claims;
using Nirote.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize(Roles = "Customer,Admin")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlist;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(IWishlistService wishlist, ILogger<WishlistController> logger)
    {
        _wishlist = wishlist;
        _logger = logger;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            _logger.LogInformation("Get wishlist for user {UserId}", UserId);
            return Ok(await _wishlist.GetWishlistAsync(UserId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get wishlist failed for user {UserId}", UserId);
            throw;
        }
    }

    [HttpPost("items")]
    public async Task<IActionResult> Add([FromBody] int productId)
    {
        try
        {
            _logger.LogInformation("Add product {ProductId} to wishlist for user {UserId}", productId, UserId);
            await _wishlist.AddToWishlistAsync(UserId, productId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add to wishlist failed for user {UserId} product {ProductId}", UserId, productId);
            throw;
        }
    }

    [HttpPost("items/remove/{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        try
        {
            _logger.LogInformation("Remove product {ProductId} from wishlist for user {UserId}", productId, UserId);
            await _wishlist.RemoveFromWishlistAsync(UserId, productId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove from wishlist failed for user {UserId} product {ProductId}", UserId, productId);
            throw;
        }
    }

    [HttpGet("check/{productId:int}")]
    public async Task<IActionResult> Check(int productId)
    {
        try
        {
            _logger.LogInformation("Check wishlist for user {UserId} product {ProductId}", UserId, productId);
            return Ok(new { isWishlisted = await _wishlist.CheckAsync(UserId, productId) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check wishlist failed for user {UserId} product {ProductId}", UserId, productId);
            throw;
        }
    }
}
