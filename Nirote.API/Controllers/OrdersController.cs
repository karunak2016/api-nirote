using System.Security.Claims;
using Nirote.Application.DTOs.Order;
using Nirote.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orders, ILogger<OrdersController> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> GetMyOrders()
    {
        try
        {
            _logger.LogInformation("Get orders for user {UserId}", UserId);
            return Ok(await _orders.GetCustomerOrdersAsync(UserId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get orders failed for user {UserId}", UserId);
            throw;
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            _logger.LogInformation("Get order {OrderId} for user {UserId}", id, UserId);
            var order = await _orders.GetByIdAsync(id, UserId);
            return order == null ? NotFound() : Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get order {OrderId} failed for user {UserId}", id, UserId);
            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
    {
        try
        {
            _logger.LogInformation("Place order for user {UserId}", UserId);
            var order = await _orders.PlaceOrderAsync(UserId, dto);
            _logger.LogInformation("Order {OrderId} placed for user {UserId}", order.Id, UserId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Place order failed for user {UserId}", UserId);
            throw;
        }
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderRequest? body = null)
    {
        try
        {
            _logger.LogInformation("Cancel order {OrderId} for user {UserId}", id, UserId);
            await _orders.CancelOrderAsync(id, UserId, body?.Reason);
            _logger.LogInformation("Order {OrderId} cancelled for user {UserId}", id, UserId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cancel order {OrderId} failed for user {UserId}", id, UserId);
            throw;
        }
    }

    [HttpGet("admin/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByIdAdmin(int id)
    {
        try
        {
            _logger.LogInformation("Admin get order {OrderId}", id);
            var order = await _orders.GetByIdAdminAsync(id);
            return order == null ? NotFound() : Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin get order {OrderId} failed", id);
            throw;
        }
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Admin get all orders page={Page}", page);
            return Ok(await _orders.GetAllOrdersAsync(page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin get all orders failed");
            throw;
        }
    }

    [HttpPost("admin/{id:int}/update-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
    {
        try
        {
            _logger.LogInformation("Update status of order {OrderId} to {Status}", id, dto.Status);
            await _orders.UpdateOrderStatusAsync(id, dto);
            _logger.LogInformation("Order {OrderId} status updated to {Status}", id, dto.Status);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update order {OrderId} status failed", id);
            throw;
        }
    }
}

public class CancelOrderRequest
{
    public string? Reason { get; set; }
}
