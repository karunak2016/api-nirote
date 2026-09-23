using System.Data;
using Dapper;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public interface IProductOptionsRepository
{
    Task<IEnumerable<ProductOption>> GetByTypeAsync(string type);
    Task<int> CreateAsync(string type, string value);
    Task UpdateAsync(int id, string value);
    Task ReorderAsync(int id, int displayOrder);
    Task DeleteAsync(int id);
}

public class ProductOptionsRepository : IProductOptionsRepository
{
    private readonly IDbConnectionFactory _db;

    public ProductOptionsRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<ProductOption>> GetByTypeAsync(string type)
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<ProductOption>(
            "sp_ProductOptions_GetByType",
            new { Type = type },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> CreateAsync(string type, string value)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@Type", type);
        p.Add("@Value", value);
        p.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        await conn.ExecuteAsync("sp_ProductOptions_Create", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewId");
    }

    public async Task UpdateAsync(int id, string value)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_ProductOptions_Update",
            new { Id = id, Value = value },
            commandType: CommandType.StoredProcedure);
    }

    public async Task ReorderAsync(int id, int displayOrder)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_ProductOptions_UpdateOrder",
            new { Id = id, DisplayOrder = displayOrder },
            commandType: CommandType.StoredProcedure);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "sp_ProductOptions_Delete",
            new { Id = id },
            commandType: CommandType.StoredProcedure);
    }
}
