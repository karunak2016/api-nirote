using System.Data;
using System.Text.Json.Serialization;
using Dapper;
using Nirote.Infrastructure.Data;
using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly UserRepository _users;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IDbConnectionFactory db, UserRepository users, ILogger<AdminController> logger)
    {
        _db = db;
        _users = users;
        _logger = logger;
    }

    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            _logger.LogInformation("Get dashboard stats");
            using var conn = _db.Create();
            using var multi = await conn.QueryMultipleAsync(
                "sp_Dashboard_GetStats",
                commandType: CommandType.StoredProcedure);

            var summary = await multi.ReadSingleOrDefaultAsync<DashboardSummaryDto>();
            var monthly = await multi.ReadAsync<MonthlyStatsDto>();
            return Ok(new { summary, monthly });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get dashboard stats failed");
            throw;
        }
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("Get customers page={Page}", page);
            var (items, total) = await _users.GetAllAsync("Customer", null, page, pageSize);
            return Ok(new { items, total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get customers failed");
            throw;
        }
    }

    [HttpGet("customers/{id:int}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        try
        {
            _logger.LogInformation("Get customer {Id}", id);
            var user = await _users.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get customer {Id} failed", id);
            throw;
        }
    }
}

public class DashboardSummaryDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int PendingOrders { get; set; }
    public int OrdersToday { get; set; }
    public decimal RevenueToday { get; set; }
    public int LowStockProducts { get; set; }
    public int ShippedOrders { get; set; }
}

public class MonthlyStatsDto
{
    public string Month { get; set; } = "";
    public decimal Revenue { get; set; }
    [JsonPropertyName("orders")]
    public int OrderCount { get; set; }
}
