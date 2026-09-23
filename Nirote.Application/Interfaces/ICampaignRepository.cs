using Nirote.Domain.Entities;

namespace Nirote.Application.Interfaces;

public interface ICampaignRepository
{
    Task<List<Campaign>> GetAllAsync();
    Task<List<Campaign>> GetActiveCampaignsAsync();
    Task<int> CreateAsync(Campaign campaign);
    Task UpdateAsync(Campaign campaign);
}
