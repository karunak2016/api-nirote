using Dapper;
using Microsoft.AspNetCore.Authorization;
using Nirote.Application.Interfaces;
using Nirote.Infrastructure.Data;
using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ISettingsRepository _settings;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(IDbConnectionFactory db, IEmailService email, IConfiguration config, ISettingsRepository settings, ILogger<NewsletterController> logger)
    {
        _db = db;
        _email = email;
        _config = config;
        _settings = settings;
        _logger = logger;
    }

    [HttpGet("subscribers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSubscribers([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(@"
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NewsletterSubscribers')
            CREATE TABLE dbo.NewsletterSubscribers (
                Id INT IDENTITY(1,1) PRIMARY KEY, Email NVARCHAR(200) NOT NULL UNIQUE,
                SubscribedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), IsActive BIT NOT NULL DEFAULT 1
            );");
        var total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.NewsletterSubscribers");
        var items = await conn.QueryAsync(
            @"SELECT Id, Email, SubscribedAt, IsActive FROM dbo.NewsletterSubscribers
              ORDER BY SubscribedAt DESC
              OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { Offset = (page - 1) * pageSize, PageSize = pageSize });
        return Ok(new { items, total });
    }

    [HttpPatch("subscribers/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleSubscriber(int id, [FromBody] ToggleRequest req)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("UPDATE dbo.NewsletterSubscribers SET IsActive = @IsActive WHERE Id = @Id",
            new { Id = id, req.IsActive });
        return Ok();
    }

    [HttpDelete("subscribers/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSubscriber(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM dbo.NewsletterSubscribers WHERE Id = @Id", new { Id = id });
        return Ok();
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return BadRequest(new { error = "Please provide a valid email address." });

        var normalised = req.Email.Trim().ToLowerInvariant();

        using var conn = _db.Create();

        // Ensure table exists
        await conn.ExecuteAsync(@"
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NewsletterSubscribers')
            CREATE TABLE dbo.NewsletterSubscribers (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Email NVARCHAR(200) NOT NULL UNIQUE,
                SubscribedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                IsActive BIT NOT NULL DEFAULT 1
            );");

        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.NewsletterSubscribers WHERE Email = @Email", new { Email = normalised });

        if (existing > 0)
            return Ok(new { message = "You're already subscribed! Thank you." });

        await conn.ExecuteAsync(
            "INSERT INTO dbo.NewsletterSubscribers (Email) VALUES (@Email)", new { Email = normalised });

        // Fetch logo before Task.Run — scoped services can't be used after request ends
        var logoSetting = await _settings.GetByKeyAsync("LogoUrl");
        var logoUrlCapture = logoSetting?.Value ?? "";

        // Send welcome email (fire and forget — don't fail the request if email fails)
        _ = Task.Run(async () =>
        {
            try
            {
                var siteName = _config["Smtp:FromName"] ?? "Niroté";
                var siteUrl = _config["SiteBaseUrl"] ?? "https://nirote.com";

                // Use img only when logo is on a public host; localhost URLs are unreachable by email clients
                var isPublicUrl = !string.IsNullOrWhiteSpace(logoUrlCapture)
                    && (logoUrlCapture.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        || logoUrlCapture.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    && !logoUrlCapture.Contains("localhost")
                    && !logoUrlCapture.Contains("127.0.0.1");

                var headerHtml = isPublicUrl
                    ? $@"<img src=""{logoUrlCapture}"" alt=""{siteName}"" style=""max-height:60px;max-width:200px;object-fit:contain"" />"
                    : $@"<span style=""font-family:Georgia,serif;font-size:28px;font-weight:bold;color:#c9a84c;letter-spacing:0.06em"">{siteName}</span>";

                var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family:sans-serif;background:#faf9f7;margin:0;padding:0"">
  <div style=""max-width:480px;margin:40px auto;background:#fff;border:1px solid #e8e3db;border-radius:12px;overflow:hidden"">
    <div style=""background:#2d3a2e;padding:28px 32px;text-align:center"">
      {headerHtml}
    </div>
    <div style=""padding:32px"">
      <h2 style=""font-family:Georgia,serif;color:#1a1008;margin:0 0 12px"">Welcome to our world!</h2>
      <p style=""color:#555;font-size:14px;line-height:1.7;margin:0 0 16px"">
        Thank you for subscribing. You'll be the first to know about new arrivals, exclusive offers, and style inspiration from {siteName}.
      </p>
      <div style=""text-align:center;margin:24px 0"">
        <a href=""{siteUrl}/products"" style=""display:inline-block;background:#2d3a2e;color:#fff;text-decoration:none;padding:12px 28px;border-radius:999px;font-size:13px;font-weight:600"">
          Shop Now
        </a>
      </div>
      <p style=""color:#aaa;font-size:12px;line-height:1.6;margin:0"">
        If you did not subscribe, you can safely ignore this email.<br/>
        <a href=""{siteUrl}"" style=""color:#aaa"">{siteName}</a>
      </p>
    </div>
  </div>
</body>
</html>";
                await _email.SendAsync(normalised, $"Welcome to {siteName}!", html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Welcome email failed for {Email}", normalised);
            }
        });

        return Ok(new { message = "You're subscribed! Welcome to the Niroté family." });
    }
}

public record SubscribeRequest(string Email);
public record ToggleRequest(bool IsActive);
