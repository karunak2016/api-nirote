using System.Data;
using Dapper;
using Nirote.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/cms/collections")]
public class CmsCollectionsController(IDbConnectionFactory db, ILogger<CmsCollectionsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_CmsCollections_GetActive", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_CmsCollections_GetAll", commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        using var conn = db.Create();
        var item = await conn.QuerySingleOrDefaultAsync("sp_CmsCollection_GetBySlug", new { Slug = slug }, commandType: CommandType.StoredProcedure);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("{slug}/products")]
    public async Task<IActionResult> GetProducts(string slug)
    {
        using var conn = db.Create();
        var items = await conn.QueryAsync("sp_Products_GetByCollection", new { CollectionSlug = slug }, commandType: CommandType.StoredProcedure);
        return Ok(items);
    }

    [HttpGet("by-product/{productId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        using var conn = db.Create();
        var ids = await conn.QueryAsync<int>("sp_ProductCollections_GetByProduct", new { ProductId = productId }, commandType: CommandType.StoredProcedure);
        return Ok(ids);
    }

    [HttpPost("set-for-product/{productId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetForProduct(int productId, [FromBody] SetProductCollectionsDto dto)
    {
        try
        {
            using var conn = db.Create();
            var json = System.Text.Json.JsonSerializer.Serialize(dto.CollectionIds);
            await conn.ExecuteAsync("sp_ProductCollections_SetForProduct", new { ProductId = productId, CollectionIds = json }, commandType: CommandType.StoredProcedure);
            return Ok();
        }
        catch (Exception ex) { logger.LogError(ex, "SetForProduct failed"); throw; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save([FromBody] CmsCollectionDto dto)
    {
        try
        {
            using var conn = db.Create();
            var p = new DynamicParameters();
            p.Add("@Id", dto.Id == 0 ? null : dto.Id);
            p.Add("@Name", dto.Name);
            p.Add("@Slug", dto.Slug);
            p.Add("@BannerUrl", dto.BannerUrl);
            p.Add("@ImageUrl", dto.ImageUrl);
            p.Add("@Description", dto.Description);
            p.Add("@SeoTitle", dto.SeoTitle);
            p.Add("@SeoDesc", dto.SeoDesc);
            p.Add("@DisplayOrder", dto.DisplayOrder);
            p.Add("@IsActive", dto.IsActive);
            p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await conn.ExecuteAsync("sp_CmsCollection_Save", p, commandType: CommandType.StoredProcedure);
            return Ok(new { id = p.Get<int>("@NewId") });
        }
        catch (Exception ex) { logger.LogError(ex, "CmsCollection save failed"); throw; }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_CmsCollection_Delete", new { Id = id }, commandType: CommandType.StoredProcedure);
        return NoContent();
    }
}

public record CmsCollectionDto(int Id, string Name, string Slug, string? BannerUrl, string? ImageUrl, string? Description, string? SeoTitle, string? SeoDesc, int DisplayOrder, bool IsActive);
public record SetProductCollectionsDto(List<int> CollectionIds);
