using System.Data;
using Dapper;
using Nirote.Application.Services;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly IDbConnectionFactory _db;

    public CartRepository(IDbConnectionFactory db) => _db = db;

    public async Task<int> GetOrCreateAsync(int userId)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@UserId", userId);
        p.Add("@CartId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Carts_GetOrCreate", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@CartId");
    }

    public async Task<(Cart? Header, IEnumerable<CartItem> Items)> GetWithItemsAsync(int userId)
    {
        using var conn = _db.Create();
        using var multi = await conn.QueryMultipleAsync(
            "sp_Carts_GetWithItems",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);

        var header = await multi.ReadSingleOrDefaultAsync<Cart>();
        var items = await multi.ReadAsync<CartItem>();
        return (header, items);
    }

    public async Task ClearAsync(int userId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Carts_Clear",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<(int CartItemId, int NewQuantity)> AddOrUpdateItemAsync(int userId, int productId, int quantity)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@UserId", userId);
        p.Add("@ProductId", productId);
        p.Add("@Quantity", quantity);
        p.Add("@CartItemId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@NewQuantity", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        p.Add("@ErrorMessage", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_CartItems_AddOrUpdate", p, commandType: CommandType.StoredProcedure);

        var success = p.Get<bool>("@Success");
        if (!success)
            throw new InvalidOperationException(p.Get<string?>("@ErrorMessage") ?? "Could not add item to cart.");

        return (p.Get<int>("@CartItemId"), p.Get<int>("@NewQuantity"));
    }

    public async Task<(bool Success, string? Error)> UpdateQuantityAsync(int cartItemId, int userId, int quantity)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@CartItemId", cartItemId);
        p.Add("@UserId", userId);
        p.Add("@Quantity", quantity);
        p.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        p.Add("@ErrorMessage", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_CartItems_UpdateQuantity", p, commandType: CommandType.StoredProcedure);
        return (p.Get<bool>("@Success"), p.Get<string?>("@ErrorMessage"));
    }

    public async Task RemoveItemAsync(int cartItemId, int userId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_CartItems_Remove",
            new { CartItemId = cartItemId, UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> GetItemCountAsync(int userId)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@UserId", userId);
        p.Add("@ItemCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_CartItems_GetCount", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@ItemCount");
    }
}
