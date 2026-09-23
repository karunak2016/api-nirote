using System.Data;
using Dapper;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class ReturnRepository : IReturnRepository
{
    private readonly IDbConnectionFactory _db;

    public ReturnRepository(IDbConnectionFactory db) => _db = db;

    public async Task<int> CreateAsync(ReturnRequest request)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@OrderId", request.OrderId);
        p.Add("@UserId", request.UserId);
        p.Add("@UserName", request.UserName);
        p.Add("@Reason", request.Reason);
        p.Add("@Description", request.Description);
        p.Add("@NewReturnId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Returns_Create", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewReturnId");
    }

    public async Task<ReturnRequest?> GetByIdAsync(int id)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<ReturnRequest>(
            "SELECT * FROM ReturnRequests WHERE Id = @Id",
            new { Id = id },
            commandType: CommandType.Text);
    }

    public async Task UpdateRefundAsync(int id, string refundStatus, string? refundId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "UPDATE ReturnRequests SET RefundStatus = @RefundStatus, RazorpayRefundId = @RefundId, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Id = id, RefundStatus = refundStatus, RefundId = refundId },
            commandType: CommandType.Text);
    }

    public async Task<IEnumerable<ReturnRequest>> GetByUserAsync(int userId)
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<ReturnRequest>(
            "sp_Returns_GetByUser",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<ReturnRequest?> GetByOrderAsync(int orderId)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<ReturnRequest>(
            "sp_Returns_GetByOrder",
            new { OrderId = orderId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<ReturnRequest>> GetAllAsync()
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<ReturnRequest>(
            "sp_Returns_GetAll",
            commandType: CommandType.StoredProcedure);
    }

    public async Task UpdateStatusAsync(int id, string status, string? adminNote)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Returns_UpdateStatus",
            new { Id = id, Status = status, AdminNote = adminNote },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<bool> ExistsForOrderAsync(int orderId)
    {
        using var conn = _db.Create();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM ReturnRequests WHERE OrderId = @OrderId",
            new { OrderId = orderId },
            commandType: CommandType.Text);
        return count > 0;
    }
}
