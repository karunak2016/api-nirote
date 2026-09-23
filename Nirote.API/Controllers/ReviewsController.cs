using System.Security.Claims;
using Nirote.Application.DTOs.Review;
using Nirote.Application.Interfaces;
using Nirote.Application.Services;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController(
    IReviewRepository reviews,
    ISettingsRepository settings,
    IOrderRepository orders,
    ILogger<ReviewsController> logger) : ControllerBase
{
    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured([FromQuery] int count = 6)
    {
        try
        {
            var list = await reviews.GetFeaturedAsync(Math.Clamp(count, 1, 20));
            return Ok(list.Select(Map));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get featured reviews failed");
            throw;
        }
    }

    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        try
        {
            var setting = await settings.GetByKeyAsync("ReviewApprovalRequired");
            var approvalRequired = setting?.Value?.ToLower() == "true";
            var list = await reviews.GetByProductAsync(productId, approvedOnly: approvalRequired);
            return Ok(list.Select(Map));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get reviews for product {ProductId} failed", productId);
            throw;
        }
    }

    [HttpGet("can-review/{productId:int}")]
    [Authorize]
    public async Task<IActionResult> CanReview(int productId)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var purchased = await orders.HasPurchasedAsync(userId, productId);
            return Ok(new { canReview = purchased });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CanReview check failed for product {ProductId}", productId);
            throw;
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        try
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5." });
            if (string.IsNullOrWhiteSpace(dto.Body))
                return BadRequest(new { error = "Review body is required." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Customer";

            var purchased = await orders.HasPurchasedAsync(userId, dto.ProductId);
            if (!purchased)
                return BadRequest(new { error = "You can only review products you have purchased." });

            var setting = await settings.GetByKeyAsync("ReviewApprovalRequired");
            var approvalRequired = setting?.Value?.ToLower() == "true";

            await reviews.CreateAsync(new Review
            {
                ProductId = dto.ProductId,
                UserId = userId,
                UserName = userName,
                Rating = dto.Rating,
                Title = dto.Title,
                Body = dto.Body,
                IsApproved = !approvalRequired,
            });

            return Ok(new { message = approvalRequired
                ? "Review submitted and awaiting approval."
                : "Review submitted successfully." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create review failed");
            throw;
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await reviews.GetAllAsync();
            return Ok(list.Select(Map));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get all reviews failed");
            throw;
        }
    }

    [HttpPost("approve/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await reviews.ApproveAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Approve review {Id} failed", id);
            throw;
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await reviews.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Delete review {Id} failed", id);
            throw;
        }
    }

    private static ReviewDto Map(Review r) => new()
    {
        Id = r.Id,
        ProductId = r.ProductId,
        UserId = r.UserId,
        UserName = r.UserName,
        Rating = r.Rating,
        Title = r.Title,
        Body = r.Body,
        IsApproved = r.IsApproved,
        CreatedAt = r.CreatedAt,
    };
}
