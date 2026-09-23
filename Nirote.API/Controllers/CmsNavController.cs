using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/nav")]
public class CmsNavController(IDbConnectionFactory db, ILogger<CmsNavController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Nav_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] NavItemDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@Label", dto.Label);
            p.Add("@Url", dto.Url);
            p.Add("@ParentId", dto.ParentId);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@IsEnabled", dto.IsEnabled);
            p.Add("@OpenInNewTab", dto.OpenInNewTab);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_Nav_Save", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "Nav save failed"); throw; }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_Nav_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }

    [HttpPost("reorder")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reorder([FromBody] List<NavReorderDto> items)
    {
        using var conn = db.Create();
        foreach (var item in items)
            await conn.ExecuteAsync("sp_Nav_Reorder", new { Id = item.Id, DisplayOrder = item.DisplayOrder }, commandType: CommandType.StoredProcedure);
        return Ok();
    }
}

public record NavItemDto(int Id, string Label, string Url, int? ParentId, int DisplayOrder, bool IsEnabled, bool OpenInNewTab);
public record NavReorderDto(int Id, int DisplayOrder);
