using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/instagram")]
public class CmsInstagramController(IDbConnectionFactory db, ILogger<CmsInstagramController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        using var conn = db.Create();
        var posts = await conn.QueryAsync("sp_Instagram_GetActive", commandType: CommandType.StoredProcedure);
        return Ok(posts);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var posts = await conn.QueryAsync("sp_Instagram_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(posts);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] InstagramPostDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@ImageUrl", dto.ImageUrl);
            p.Add("@PostUrl", dto.PostUrl);
            p.Add("@Caption", dto.Caption);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@IsActive", dto.IsActive);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_Instagram_Save", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "Instagram save failed"); throw; }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_Instagram_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }
}

public record InstagramPostDto(int Id, string ImageUrl, string? PostUrl, string? Caption, int DisplayOrder, bool IsActive);
