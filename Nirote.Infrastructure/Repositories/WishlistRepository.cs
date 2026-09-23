using System.Data;
using Dapper;
using Nirote.Application.Services;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly IDbConnectionFactory _db;

    public WishlistRepository(IDbConnectionFactory db) => _db = db;

    public async Task AddAsync(int userId, int productId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Wishlists_Add",
            new { UserId = userId, ProductId = productId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task RemoveAsync(int userId, int productId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Wishlists_Remove",
            new { UserId = userId, ProductId = productId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<WishlistItemDto>> GetByUserAsync(int userId)
    {
        using var conn = _db.Create();
        var rows = await conn.QueryAsync("sp_Wishlists_GetByUser",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);

        return rows.Select(row =>
        {
            var d = (IDictionary<string, object>)row;
            d.TryGetValue("ProductId", out var pid);
            d.TryGetValue("ProductName", out var pname);
            d.TryGetValue("ImageUrl", out var imgUrl);
            d.TryGetValue("DefaultImage", out var defImg);
            d.TryGetValue("Price", out var price);
            return new WishlistItemDto(
                Convert.ToInt32(pid),
                (string?)pname ?? "",
                (string?)imgUrl ?? (string?)defImg,
                Convert.ToDecimal(price)
            );
        });
    }

    public async Task<bool> CheckAsync(int userId, int productId)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@UserId", userId);
        p.Add("@ProductId", productId);
        p.Add("@IsWishlisted", dbType: DbType.Boolean, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Wishlists_Check", p, commandType: CommandType.StoredProcedure);
        return p.Get<bool>("@IsWishlisted");
    }
}
