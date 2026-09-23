using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/sections")]
public class CmsSectionsController(IDbConnectionFactory db, ILogger<CmsSectionsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_HomeSections_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] HomeSectionDto dto)
    {
        try
        {
            using var conn = db.Create();
            await conn.ExecuteAsync("sp_HomeSections_Save", new
            {
                dto.SectionKey, dto.Heading, dto.Subheading, dto.CtaText, dto.CtaUrl, dto.IsEnabled, dto.DisplayOrder
            }, commandType: CommandType.StoredProcedure);
            return Ok();
        }
        catch (Exception ex) { logger.LogError(ex, "HomeSections save failed"); throw; }
    }

    [HttpPost("reorder")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reorder([FromBody] List<SectionOrderDto> items)
    {
        try
        {
            using var conn = db.Create();
            foreach (var item in items)
                await conn.ExecuteAsync(
                    "UPDATE dbo.HomeSections SET DisplayOrder=@Order WHERE SectionKey=@Key",
                    new { Key = item.SectionKey, Order = item.DisplayOrder },
                    commandType: CommandType.Text);
            return Ok();
        }
        catch (Exception ex) { logger.LogError(ex, "HomeSections reorder failed"); throw; }
    }

    [HttpPost("toggle/{sectionKey}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(string sectionKey, [FromBody] ToggleDto dto)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync(
            "UPDATE dbo.HomeSections SET IsEnabled=@IsEnabled WHERE SectionKey=@Key",
            new { Key = sectionKey, dto.IsEnabled },
            commandType: CommandType.Text);
        return Ok();
    }
}

public record HomeSectionDto(string SectionKey, string? Heading, string? Subheading, string? CtaText, string? CtaUrl, bool IsEnabled, int DisplayOrder);
public record SectionOrderDto(string SectionKey, int DisplayOrder);
public record ToggleDto(bool IsEnabled);
