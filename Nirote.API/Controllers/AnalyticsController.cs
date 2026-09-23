using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nirote.Infrastructure.Data;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public AnalyticsController(IDbConnectionFactory db) => _db = db;

    // Public — called by storefront on product page load
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] TrackViewRequest req)
    {
        if (req.ProductId <= 0) return BadRequest();
        var source = ParseSource(req.Referrer);
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Analytics_TrackView",
            new { req.ProductId, Source = source, req.SessionId, req.UserId },
            commandType: CommandType.StoredProcedure);
        return Ok();
    }

    [HttpGet("product/{productId:int}/viewers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ProductViewers(int productId, [FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(@"
            SELECT
                pv.Id,
                pv.ViewedAt,
                pv.Source,
                pv.SessionId,
                pv.UserId,
                u.Name  AS UserName,
                u.Email AS UserEmail
            FROM dbo.ProductViews pv
            LEFT JOIN dbo.Users u ON u.Id = pv.UserId
            WHERE pv.ProductId = @ProductId
              AND pv.ViewedAt >= DATEADD(day, -@Days, SYSUTCDATETIME())
            ORDER BY pv.ViewedAt DESC",
            new { ProductId = productId, Days = days });
        return Ok(rows);
    }

    [HttpGet("summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Summary([FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var row = await conn.QuerySingleOrDefaultAsync(
            "sp_Analytics_GetSummary", new { Days = days },
            commandType: CommandType.StoredProcedure);
        return Ok(row);
    }

    [HttpGet("daily-views")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DailyViews([FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "sp_Analytics_GetDailyViews", new { Days = days },
            commandType: CommandType.StoredProcedure);
        return Ok(rows);
    }

    [HttpGet("top-products/views")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TopByViews([FromQuery] int top = 10, [FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "sp_Analytics_GetTopProductsByViews", new { Top = top, Days = days },
            commandType: CommandType.StoredProcedure);
        return Ok(rows);
    }

    [HttpGet("top-products/sales")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TopBySales([FromQuery] int top = 10, [FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "sp_Analytics_GetTopProductsBySales", new { Top = top, Days = days },
            commandType: CommandType.StoredProcedure);
        return Ok(rows);
    }

    [HttpGet("daily-orders")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DailyOrders([FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "sp_Analytics_GetDailyOrders", new { Days = days },
            commandType: CommandType.StoredProcedure);
        return Ok(rows);
    }

    [HttpGet("daily-revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DailyRevenue([FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "sp_Analytics_GetDailyRevenue", new { Days = days },
            commandType: CommandType.StoredProcedure);
        return Ok(rows);
    }

    [HttpGet("traffic-sources")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> TrafficSources([FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync(
            "sp_Analytics_GetTrafficSources", new { Days = days },
            commandType: CommandType.StoredProcedure);
        return Ok(rows);
    }

    // Normalise referrer URL → source label
    private static string? ParseSource(string? referrer)
    {
        if (string.IsNullOrWhiteSpace(referrer)) return null;
        if (!Uri.TryCreate(referrer, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host.ToLowerInvariant().Replace("www.", "");
        return host switch
        {
            var h when h.Contains("google")    => "Google",
            var h when h.Contains("instagram") => "Instagram",
            var h when h.Contains("facebook")  => "Facebook",
            var h when h.Contains("youtube")   => "YouTube",
            var h when h.Contains("twitter") || h.Contains("x.com") => "Twitter/X",
            var h when h.Contains("whatsapp")  => "WhatsApp",
            _ => host
        };
    }
}

public class TrackViewRequest
{
    public int     ProductId { get; set; }
    public string? Referrer  { get; set; }
    public string? SessionId { get; set; }
    public int?    UserId    { get; set; }
}
