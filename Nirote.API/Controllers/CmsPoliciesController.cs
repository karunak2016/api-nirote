using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/policies")]
public class CmsPoliciesController(IDbConnectionFactory db, ILogger<CmsPoliciesController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Policy_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        using var conn = db.Create();
        var item = await conn.QuerySingleOrDefaultAsync("sp_Policy_Get", new { Slug = slug }, commandType: CommandType.StoredProcedure);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{slug}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save(string slug, [FromBody] PolicyDto dto)
    {
        try
        {
            using var conn = db.Create();
            await conn.ExecuteAsync("sp_Policy_Save", new { Slug = slug, dto.Title, dto.Sections }, commandType: CommandType.StoredProcedure);
            return Ok();
        }
        catch (Exception ex) { logger.LogError(ex, "Policy save failed: {Slug}", slug); throw; }
    }
}

public record PolicyDto(string Title, string Sections);
