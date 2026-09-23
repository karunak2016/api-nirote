using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/popups")]
public class CmsPopupsController(IDbConnectionFactory db, ILogger<CmsPopupsController> logger) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Popups_GetActive", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Popups_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpPost("{popupType}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save(string popupType, [FromBody] PopupDto dto)
    {
        try
        {
            using var conn = db.Create();
            await conn.ExecuteAsync("sp_Popup_Save", new
            {
                PopupType = popupType,
                dto.Heading, dto.Subheading, dto.ImageUrl, dto.ButtonText, dto.ButtonUrl,
                dto.CouponCode, dto.IsEnabled, dto.TriggerDelay
            }, commandType: CommandType.StoredProcedure);
            return Ok();
        }
        catch (Exception ex) { logger.LogError(ex, "Popup save failed: {Type}", popupType); throw; }
    }
}

public record PopupDto(string? Heading, string? Subheading, string? ImageUrl, string? ButtonText, string? ButtonUrl, string? CouponCode, bool IsEnabled, int TriggerDelay);
