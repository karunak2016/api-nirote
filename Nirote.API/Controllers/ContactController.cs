using Dapper;
using Microsoft.AspNetCore.Authorization;
using Nirote.Application.Interfaces;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IDbConnectionFactory db, IEmailService email, IConfiguration config, ILogger<ContactController> logger)
    {
        _db = db;
        _email = email;
        _config = config;
        _logger = logger;
    }

    private const string EnsureTable = @"
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ContactMessages')
        CREATE TABLE dbo.ContactMessages (
            Id          INT IDENTITY(1,1) PRIMARY KEY,
            Name        NVARCHAR(200)  NOT NULL,
            Email       NVARCHAR(200)  NOT NULL,
            Phone       NVARCHAR(50)   NULL,
            Subject     NVARCHAR(300)  NULL,
            Message     NVARCHAR(MAX)  NOT NULL,
            IsRead      BIT            NOT NULL DEFAULT 0,
            SubmittedAt DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
        );";

    // ── Admin endpoints ─────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string filter = "all")
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(EnsureTable);

        var where = filter switch {
            "unread" => "WHERE IsRead = 0",
            "read"   => "WHERE IsRead = 1",
            _        => ""
        };

        var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.ContactMessages {where}");
        var items = await conn.QueryAsync(
            $@"SELECT Id, Name, Email, Phone, Subject, Message, IsRead, SubmittedAt
               FROM dbo.ContactMessages {where} ORDER BY SubmittedAt DESC
               OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
            new { Offset = (page - 1) * pageSize, PageSize = pageSize });
        return Ok(new { items, total });
    }

    [HttpPatch("{id:int}/read")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkRead(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("UPDATE dbo.ContactMessages SET IsRead = 1 WHERE Id = @Id", new { Id = id });
        return Ok();
    }

    [HttpPatch("read-all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkAllRead()
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("UPDATE dbo.ContactMessages SET IsRead = 1 WHERE IsRead = 0");
        return Ok();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM dbo.ContactMessages WHERE Id = @Id", new { Id = id });
        return Ok();
    }

    [HttpDelete("bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBulk([FromBody] BulkDeleteRequest req)
    {
        if (req.Ids == null || req.Ids.Count == 0) return BadRequest();
        using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM dbo.ContactMessages WHERE Id IN @Ids", new { Ids = req.Ids });
        return Ok(new { deleted = req.Ids.Count });
    }

    [HttpDelete("cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Cleanup([FromQuery] int days = 30)
    {
        using var conn = _db.Create();
        var deleted = await conn.ExecuteAsync(
            "DELETE FROM dbo.ContactMessages WHERE IsRead = 1 AND SubmittedAt < DATEADD(day, -@Days, SYSUTCDATETIME())",
            new { Days = days });
        return Ok(new { deleted });
    }

    [HttpPost("{id:int}/reply")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Reply message is required." });

        using var conn = _db.Create();
        var original = await conn.QuerySingleOrDefaultAsync<ContactMessageRow>(
            "SELECT Name, Email, Subject, Message FROM dbo.ContactMessages WHERE Id = @Id", new { Id = id });

        if (original == null) return NotFound();

        var siteName  = _config["Smtp:FromName"] ?? "Niroté";
        var subject   = $"Re: {(string.IsNullOrWhiteSpace(original.Subject) ? "Your Enquiry" : original.Subject)}";
        var replyHtml = System.Net.WebUtility.HtmlEncode(req.Message).Replace("\n", "<br/>");
        var origHtml  = System.Net.WebUtility.HtmlEncode(original.Message).Replace("\n", "<br/>");

        var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family:sans-serif;background:#faf9f7;margin:0;padding:0"">
  <div style=""max-width:520px;margin:40px auto;background:#fff;border:1px solid #e8e3db;border-radius:12px;overflow:hidden"">
    <div style=""background:#1E1E1E;padding:24px 32px"">
      <span style=""font-family:Georgia,serif;font-size:22px;font-weight:bold;color:#C9A227;letter-spacing:0.08em"">{siteName}</span>
    </div>
    <div style=""padding:28px 32px"">
      <p style=""font-size:15px;color:#1E1E1E;margin:0 0 8px"">Hi {System.Net.WebUtility.HtmlEncode(original.Name)},</p>
      <div style=""font-size:14px;color:#444;line-height:1.75;margin-bottom:28px"">{replyHtml}</div>
      <p style=""font-size:13px;color:#888;margin:0"">Warm regards,<br/><strong style=""color:#1E1E1E"">{siteName} Team</strong></p>
      <div style=""margin-top:28px;padding-top:20px;border-top:1px solid #e8e3db"">
        <p style=""font-size:11px;color:#aaa;margin:0 0 6px"">Your original message:</p>
        <p style=""font-size:13px;color:#888;line-height:1.6;margin:0"">{origHtml}</p>
      </div>
    </div>
  </div>
</body>
</html>";

        try
        {
            await _email.SendAsync(original.Email, subject, html);
            await conn.ExecuteAsync("UPDATE dbo.ContactMessages SET IsRead = 1 WHERE Id = @Id", new { Id = id });
            return Ok(new { message = "Reply sent successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reply to {Email}", original.Email);
            return StatusCode(500, new { error = "Failed to send email. Please check SMTP settings." });
        }
    }

    // ── Public submit ────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ContactRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Name, email and message are required." });

        if (!req.Email.Contains('@'))
            return BadRequest(new { error = "Please provide a valid email address." });

        using var conn = _db.Create();
        await conn.ExecuteAsync(EnsureTable);

        await conn.ExecuteAsync(@"
            INSERT INTO dbo.ContactMessages (Name, Email, Phone, Subject, Message)
            VALUES (@Name, @Email, @Phone, @Subject, @Message)",
            new { req.Name, req.Email, req.Phone, req.Subject, req.Message });

        _logger.LogInformation("Contact form submission from {Email}", req.Email);

        // Fire-and-forget admin notification email
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = _config["Smtp:AdminEmail"] ?? _config["Smtp:From"] ?? _config["Smtp:Username"] ?? "";
                if (string.IsNullOrWhiteSpace(adminEmail)) return;

                var siteName = _config["Smtp:FromName"] ?? "Niroté";
                var subject  = string.IsNullOrWhiteSpace(req.Subject) ? "Contact Form Enquiry" : req.Subject;
                var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family:sans-serif;background:#faf9f7;margin:0;padding:0"">
  <div style=""max-width:520px;margin:40px auto;background:#fff;border:1px solid #e8e3db;border-radius:12px;overflow:hidden"">
    <div style=""background:#1E1E1E;padding:24px 32px"">
      <span style=""font-family:Georgia,serif;font-size:22px;font-weight:bold;color:#C9A227;letter-spacing:0.08em"">{siteName}</span>
      <span style=""color:#aaa;font-size:13px;margin-left:12px"">New Contact Message</span>
    </div>
    <div style=""padding:28px 32px"">
      <table style=""width:100%;border-collapse:collapse;font-size:14px"">
        <tr><td style=""color:#999;padding:6px 0;width:90px"">Name</td><td style=""color:#1E1E1E;font-weight:600"">{System.Net.WebUtility.HtmlEncode(req.Name)}</td></tr>
        <tr><td style=""color:#999;padding:6px 0"">Email</td><td><a href=""mailto:{System.Net.WebUtility.HtmlEncode(req.Email)}"" style=""color:#C9A227"">{System.Net.WebUtility.HtmlEncode(req.Email)}</a></td></tr>
        {(string.IsNullOrWhiteSpace(req.Phone) ? "" : $"<tr><td style=\"color:#999;padding:6px 0\">Phone</td><td style=\"color:#1E1E1E\">{System.Net.WebUtility.HtmlEncode(req.Phone)}</td></tr>")}
        {(string.IsNullOrWhiteSpace(req.Subject) ? "" : $"<tr><td style=\"color:#999;padding:6px 0\">Subject</td><td style=\"color:#1E1E1E\">{System.Net.WebUtility.HtmlEncode(req.Subject)}</td></tr>")}
      </table>
      <div style=""margin-top:16px;padding:16px;background:#faf9f7;border-radius:8px;border:1px solid #e8e3db"">
        <p style=""color:#555;font-size:14px;line-height:1.7;margin:0"">{System.Net.WebUtility.HtmlEncode(req.Message).Replace("\n", "<br/>")}</p>
      </div>
      <p style=""margin-top:20px"">
        <a href=""mailto:{System.Net.WebUtility.HtmlEncode(req.Email)}"" style=""display:inline-block;background:#C9A227;color:#fff;text-decoration:none;padding:10px 24px;border-radius:999px;font-size:13px;font-weight:600"">
          Reply to {System.Net.WebUtility.HtmlEncode(req.Name)}
        </a>
      </p>
    </div>
  </div>
</body>
</html>";

                await _email.SendAsync(adminEmail, $"[{siteName}] {subject} – from {req.Name}", html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin contact notification email failed (non-fatal)");
            }
        });

        return Ok(new { message = "Thank you! Your message has been sent. We'll get back to you within 24 hours." });
    }
}

public record ContactRequest(string Name, string Email, string? Phone, string? Subject, string Message);
public record ReplyRequest(string Message);
public record ContactMessageRow(string Name, string Email, string? Subject, string Message);
public record BulkDeleteRequest(List<int> Ids);
