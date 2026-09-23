using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/banners")]
public class CmsBannersController(IDbConnectionFactory db, ILogger<CmsBannersController> logger) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Banners_GetActive_V2", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Banners_GetAll_V2", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] BannerDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@Badge", dto.Badge);
            p.Add("@Heading", dto.Heading);
            p.Add("@Subheading", dto.Subheading);
            p.Add("@ImageUrl", dto.ImageUrl);
            p.Add("@MobileImageUrl", dto.MobileImageUrl);
            p.Add("@TextAlign", dto.TextAlign ?? "left");
            p.Add("@Btn1Text", dto.Btn1Text);
            p.Add("@Btn1Url", dto.Btn1Url);
            p.Add("@Btn2Text", dto.Btn2Text);
            p.Add("@Btn2Url", dto.Btn2Url);
            p.Add("@IsActive", dto.IsActive);
            p.Add("@ScheduledStart", dto.ScheduledStart);
            p.Add("@ScheduledEnd", dto.ScheduledEnd);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@Priority", dto.Priority);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_Banners_Save_V2", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "Banner save failed"); throw; }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_Banners_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }
}

public record BannerDto(int Id, string? Badge, string Heading, string? Subheading, string? ImageUrl, string? MobileImageUrl,
    string? TextAlign, string? Btn1Text, string? Btn1Url, string? Btn2Text, string? Btn2Url,
    bool IsActive, DateTime? ScheduledStart, DateTime? ScheduledEnd, int DisplayOrder, int Priority);
