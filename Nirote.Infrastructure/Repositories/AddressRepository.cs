using System.Data;
using Dapper;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class AddressRepository
{
    private readonly IDbConnectionFactory _db;

    public AddressRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Address>> GetByUserAsync(int userId)
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<Address>("sp_Addresses_GetByUser",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Address?> GetByIdAsync(int addressId)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Address>("sp_Addresses_GetById",
            new { AddressId = addressId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> CreateAsync(Address a)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@UserId", a.UserId);
        p.Add("@FullName", a.FullName);
        p.Add("@Phone", a.Phone);
        p.Add("@Line1", a.Line1);
        p.Add("@Line2", a.Line2);
        p.Add("@City", a.City);
        p.Add("@State", a.State);
        p.Add("@Pincode", a.Pincode);
        p.Add("@Country", a.Country);
        p.Add("@IsDefault", a.IsDefault);
        p.Add("@NewAddressId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Addresses_Create", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewAddressId");
    }

    public async Task UpdateAsync(Address a)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Addresses_Update", new
        {
            AddressId = a.Id,
            a.FullName,
            a.Phone,
            a.Line1,
            a.Line2,
            a.City,
            a.State,
            a.Pincode,
            a.Country,
            a.IsDefault
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task SetDefaultAsync(int addressId, int userId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Addresses_SetDefault",
            new { AddressId = addressId, UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<string?> DeleteAsync(int addressId)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@AddressId", addressId);
        p.Add("@ErrorMessage", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Addresses_Delete", p, commandType: CommandType.StoredProcedure);
        return p.Get<string?>("@ErrorMessage");
    }
}
