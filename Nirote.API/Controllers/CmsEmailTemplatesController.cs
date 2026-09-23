using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/email-templates")]
[Authorize(Roles = "Admin")]
public class CmsEmailTemplatesController(IDbConnectionFactory db, ILogger<CmsEmailTemplatesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_EmailTemplates_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("{templateType}")]
    public async Task<IActionResult> Get(string templateType)
    {
        using var conn = db.Create();
        var item = await conn.QuerySingleOrDefaultAsync("sp_EmailTemplate_GetByType", new { TemplateType = templateType }, commandType: CommandType.StoredProcedure);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{templateType}")]
    public async Task<IActionResult> Save(string templateType, [FromBody] EmailTemplateDto dto)
    {
        try
        {
            using var conn = db.Create();
            await conn.ExecuteAsync("sp_EmailTemplate_Save", new { TemplateType = templateType, dto.Subject, dto.Body, dto.IsEnabled }, commandType: CommandType.StoredProcedure);
            return Ok();
        }
        catch (Exception ex) { logger.LogError(ex, "Email template save failed: {Type}", templateType); throw; }
    }
}

public record EmailTemplateDto(string Subject, string Body, bool IsEnabled);
