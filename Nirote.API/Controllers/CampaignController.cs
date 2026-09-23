using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/campaigns")]
public class CampaignController(ICampaignRepository repo, ILogger<CampaignController> logger) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var campaigns = await repo.GetActiveCampaignsAsync();
        return Ok(campaigns);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var campaigns = await repo.GetAllAsync();
        return Ok(campaigns);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CampaignDto dto)
    {
        var campaign = new Campaign
        {
            Name = dto.Name, Description = dto.Description, CategoryId = dto.CategoryId,
            DiscountType = dto.DiscountType, DiscountValue = dto.DiscountValue,
            StartDate = dto.StartDate, EndDate = dto.EndDate,
        };
        campaign.Id = await repo.CreateAsync(campaign);
        logger.LogInformation("Campaign {Name} created with Id {Id}", campaign.Name, campaign.Id);
        return Ok(campaign);
    }

    [HttpPost("update/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCampaignDto dto)
    {
        var campaign = new Campaign
        {
            Id = id, Name = dto.Name, Description = dto.Description, CategoryId = dto.CategoryId,
            DiscountType = dto.DiscountType, DiscountValue = dto.DiscountValue,
            StartDate = dto.StartDate, EndDate = dto.EndDate, IsActive = dto.IsActive,
        };
        await repo.UpdateAsync(campaign);
        logger.LogInformation("Campaign {Id} updated", id);
        return Ok(campaign);
    }
}

public class CampaignDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateCampaignDto : CampaignDto
{
    public bool IsActive { get; set; } = true;
}
