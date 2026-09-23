using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/options")]
public class ProductOptionsController : ControllerBase
{
    private readonly IProductOptionsRepository _repo;

    public ProductOptionsController(IProductOptionsRepository repo) => _repo = repo;

    [HttpGet("fabrics")]
    public async Task<IActionResult> GetFabrics() =>
        Ok(await _repo.GetByTypeAsync("Fabric"));

    [HttpGet("colors")]
    public async Task<IActionResult> GetColors() =>
        Ok(await _repo.GetByTypeAsync("Color"));

    [HttpPost("fabrics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFabric([FromBody] OptionValueDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Value is required.");
        var id = await _repo.CreateAsync("Fabric", dto.Value.Trim());
        return Ok(new { id, type = "Fabric", value = dto.Value.Trim() });
    }

    [HttpPost("colors")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateColor([FromBody] OptionValueDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Value is required.");
        var id = await _repo.CreateAsync("Color", dto.Value.Trim());
        return Ok(new { id, type = "Color", value = dto.Value.Trim() });
    }

    [HttpPost("fabrics/update/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateFabric(int id, [FromBody] OptionValueDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Value is required.");
        await _repo.UpdateAsync(id, dto.Value.Trim());
        return Ok(new { id, type = "Fabric", value = dto.Value.Trim() });
    }

    [HttpPost("colors/update/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateColor(int id, [FromBody] OptionValueDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Value)) return BadRequest("Value is required.");
        await _repo.UpdateAsync(id, dto.Value.Trim());
        return Ok(new { id, type = "Color", value = dto.Value.Trim() });
    }

    [HttpPost("fabrics/reorder/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReorderFabric(int id, [FromBody] ReorderDto dto)
    {
        await _repo.ReorderAsync(id, dto.DisplayOrder);
        return Ok();
    }

    [HttpPost("colors/reorder/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReorderColor(int id, [FromBody] ReorderDto dto)
    {
        await _repo.ReorderAsync(id, dto.DisplayOrder);
        return Ok();
    }

    [HttpPost("fabrics/delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFabric(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("colors/delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteColor(int id)
    {
        await _repo.DeleteAsync(id);
        return NoContent();
    }
}

public class OptionValueDto
{
    public string Value { get; set; } = string.Empty;
}

public class ReorderDto
{
    public int DisplayOrder { get; set; }
}
