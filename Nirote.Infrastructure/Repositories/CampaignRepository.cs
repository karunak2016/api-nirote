using System.Data;
using Dapper;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class CampaignRepository(IDbConnectionFactory db) : ICampaignRepository
{
    public async Task<List<Campaign>> GetAllAsync()
    {
        using var conn = db.Create();
        var rows = await conn.QueryAsync<Campaign>("sp_Campaigns_GetAll", commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<List<Campaign>> GetActiveCampaignsAsync()
    {
        using var conn = db.Create();
        var rows = await conn.QueryAsync<Campaign>("sp_Campaigns_GetActive", commandType: CommandType.StoredProcedure);
        return rows.ToList();
    }

    public async Task<int> CreateAsync(Campaign campaign)
    {
        using var conn = db.Create();
        var p = new DynamicParameters();
        p.Add("@Name",          campaign.Name);
        p.Add("@Description",   campaign.Description);
        p.Add("@CategoryId",    campaign.CategoryId);
        p.Add("@DiscountType",  campaign.DiscountType);
        p.Add("@DiscountValue", campaign.DiscountValue);
        p.Add("@StartDate",     campaign.StartDate);
        p.Add("@EndDate",       campaign.EndDate);
        p.Add("@NewCampaignId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await conn.ExecuteAsync("sp_Campaigns_Create", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewCampaignId");
    }

    public async Task UpdateAsync(Campaign campaign)
    {
        using var conn = db.Create();
        await conn.ExecuteAsync("sp_Campaigns_Update", new
        {
            campaign.Id, campaign.Name, campaign.Description, campaign.CategoryId,
            campaign.DiscountType, campaign.DiscountValue,
            campaign.StartDate, campaign.EndDate, campaign.IsActive
        }, commandType: CommandType.StoredProcedure);
    }
}
