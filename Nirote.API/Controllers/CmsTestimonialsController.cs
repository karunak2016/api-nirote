using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/testimonials")]
public class CmsTestimonialsController(IDbConnectionFactory db, ILogger<CmsTestimonialsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Testimonials_GetActive", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Testimonials_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] TestimonialDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@AuthorName", dto.AuthorName);
            p.Add("@AuthorCity", dto.AuthorCity);
            p.Add("@AuthorImage", dto.AuthorImage);
            p.Add("@Text", dto.Text);
            p.Add("@Rating", dto.Rating);
            p.Add("@IsActive", dto.IsActive);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_Testimonials_Save", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "Testimonial save failed"); throw; }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_Testimonials_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }
}

public record TestimonialDto(int Id, string AuthorName, string? AuthorCity, string? AuthorImage, string Text, int Rating, bool IsActive, int DisplayOrder);
