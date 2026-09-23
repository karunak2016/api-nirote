using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/faq")]
public class CmsFaqController(IDbConnectionFactory db, ILogger<CmsFaqController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        using var conn = db.Create();
        using var multi = await conn.QueryMultipleAsync("sp_Faq_GetAll", commandType: CommandType.StoredProcedure);
        var categories = (await multi.ReadAsync()).ToList();
        var items = (await multi.ReadAsync()).ToList();
        return Ok(new { categories, items });
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        using var multi = await conn.QueryMultipleAsync("sp_Faq_GetAllAdmin", commandType: CommandType.StoredProcedure);
        var categories = (await multi.ReadAsync()).ToList();
        var items = (await multi.ReadAsync()).ToList();
        return Ok(new { categories, items });
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveCategory([FromBody] FaqCategoryDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@Name", dto.Name);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@IsActive", dto.IsActive);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_FaqCategory_Save", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "Faq category save failed"); throw; }
    }

    [HttpPost("categories/delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_FaqCategory_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }

    [HttpPost("items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveItem([FromBody] FaqItemDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@CategoryId", dto.CategoryId);
            p.Add("@Question", dto.Question);
            p.Add("@Answer", dto.Answer);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@IsActive", dto.IsActive);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_FaqItem_Save", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "Faq item save failed"); throw; }
    }

    [HttpPost("items/delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_FaqItem_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }
}

public record FaqCategoryDto(int Id, string Name, int DisplayOrder, bool IsActive);
public record FaqItemDto(int Id, int CategoryId, string Question, string Answer, int DisplayOrder, bool IsActive);
