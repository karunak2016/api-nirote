using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/seo")]
public class CmsSeoController(IDbConnectionFactory db, ILogger<CmsSeoController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Seo_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("{pageKey}")]
    public async Task<IActionResult> Get(string pageKey)
    {
        using var conn = db.Create();
        var item = await conn.QuerySingleOrDefaultAsync("sp_Seo_Get", new { PageKey = pageKey }, commandType: CommandType.StoredProcedure);
        return Ok(item);
    }

    [HttpPost("{pageKey}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save(string pageKey, [FromBody] SeoDto dto)
    {
        try
        {
            using var conn = db.Create();
            await conn.ExecuteAsync("sp_Seo_Save", new { PageKey = pageKey, dto.MetaTitle, dto.MetaDesc, dto.Keywords, dto.OgImage }, commandType: CommandType.StoredProcedure);
            return Ok();
        }
        catch (Exception ex) { logger.LogError(ex, "SEO save failed: {PageKey}", pageKey); throw; }
    }
}

public record SeoDto(string? MetaTitle, string? MetaDesc, string? Keywords, string? OgImage);
