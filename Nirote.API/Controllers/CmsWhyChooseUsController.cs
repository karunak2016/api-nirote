using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/why-choose-us")]
public class CmsWhyChooseUsController(IDbConnectionFactory db, ILogger<CmsWhyChooseUsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_WhyChooseUs_GetActive", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_WhyChooseUs_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] WhyChooseUsDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@IconName", dto.IconName);
            p.Add("@Title", dto.Title);
            p.Add("@Description", dto.Description);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@IsActive", dto.IsActive);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_WhyChooseUs_Save", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "WhyChooseUs save failed"); throw; }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_WhyChooseUs_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }
}

public record WhyChooseUsDto(int Id, string IconName, string Title, string Description, int DisplayOrder, bool IsActive);
