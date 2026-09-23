using System.Data;
using Dapper;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public interface ISettingsRepository
{
    Task<Setting?> GetByKeyAsync(string key);
    Task SetAsync(string key, string value);
}

public class SettingsRepository : ISettingsRepository
{
    private readonly IDbConnectionFactory _db;

    public SettingsRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Setting>(
            "sp_Settings_GetByKey",
            new { Key = key },
            commandType: CommandType.StoredProcedure);
    }

    public async Task SetAsync(string key, string value)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_Settings_Set",
            new { Key = key, Value = value },
            commandType: CommandType.StoredProcedure);
    }
}
