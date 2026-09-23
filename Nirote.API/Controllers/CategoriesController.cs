using System.Data;
using Dapper;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IDbConnectionFactory db, ILogger<CategoriesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            _logger.LogInformation("GetAll categories includeInactive={IncludeInactive}", includeInactive);
            using var conn = _db.Create();
            var result = await conn.QueryAsync<Category>("sp_Categories_GetAll",
                new { IncludeInactive = includeInactive },
                commandType: CommandType.StoredProcedure);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll categories failed");
            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        try
        {
            _logger.LogInformation("Creating category: {Name}", dto.Name);
            using var conn = _db.Create();
            var p = new DynamicParameters();
            p.Add("@Name", dto.Name);
            p.Add("@Slug", dto.Slug);
            p.Add("@Description", dto.Description);
            p.Add("@ImageUrl", dto.ImageUrl);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@ParentId", dto.ParentId);
            p.Add("@ShowOnHomepage", dto.ShowOnHomepage);
            p.Add("@NewCategoryId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_Categories_Create", p, commandType: CommandType.StoredProcedure);
            var newId = p.Get<int>("@NewCategoryId");
            var created = await conn.QuerySingleOrDefaultAsync<Category>(
                "SELECT * FROM Categories WHERE Id = @Id", new { Id = newId }, commandType: CommandType.Text);
            _logger.LogInformation("Category created with Id {Id}", newId);
            return Ok(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create category failed: {Name}", dto.Name);
            throw;
        }
    }

    [HttpPost("update/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UpdateCategoryDto dto)
    {
        try
        {
            _logger.LogInformation("Updating category {Id}", id);
            using var conn = _db.Create();
            await conn.ExecuteAsync("sp_Categories_Update", new
            {
                CategoryId = id,
                dto.Name, dto.Slug, dto.Description, dto.ImageUrl, dto.DisplayOrder, dto.IsActive, dto.ParentId, dto.ShowOnHomepage
            }, commandType: CommandType.StoredProcedure);
            var updated = await conn.QuerySingleOrDefaultAsync<Category>(
                "SELECT * FROM Categories WHERE Id = @Id", new { Id = id }, commandType: CommandType.Text);
            _logger.LogInformation("Category {Id} updated", id);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update category {Id} failed", id);
            throw;
        }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            _logger.LogInformation("Deleting category {Id}", id);
            using var conn = _db.Create();
            var p = new DynamicParameters();
            p.Add("@CategoryId", id);
            p.Add("@ErrorMessage", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_Categories_Delete", p, commandType: CommandType.StoredProcedure);
            var error = p.Get<string?>("@ErrorMessage");
            if (!string.IsNullOrEmpty(error)) return BadRequest(new { error });
            _logger.LogInformation("Category {Id} deleted", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete category {Id} failed", id);
            throw;
        }
    }
}

public record CreateCategoryDto(string Name, string Slug, string? Description, string? ImageUrl, int DisplayOrder, int? ParentId, bool ShowOnHomepage = false);
public record UpdateCategoryDto(string Name, string Slug, string? Description, string? ImageUrl, int DisplayOrder, bool IsActive, int? ParentId, bool ShowOnHomepage = false);
