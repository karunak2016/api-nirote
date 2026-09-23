using System.Data;
using System.Text.Json;
using Dapper;
using Nirote.Application.Services;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;

namespace Nirote.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _db;

    public OrderRepository(IDbConnectionFactory db) => _db = db;

    public async Task<(IEnumerable<Order> Items, int Total)> GetByUserAsync(int userId, int page, int pageSize)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@UserId", userId);
        p.Add("@Page", page);
        p.Add("@PageSize", pageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = await conn.QueryAsync<Order>("sp_Orders_GetByUser", p, commandType: CommandType.StoredProcedure);
        return (items, p.Get<int>("@TotalCount"));
    }

    public async Task<(Order? Header, IEnumerable<OrderItem> Items)> GetByIdAsync(int orderId)
    {
        using var conn = _db.Create();
        using var multi = await conn.QueryMultipleAsync(
            "sp_Orders_GetById",
            new { OrderId = orderId },
            commandType: CommandType.StoredProcedure);

        var header = await multi.ReadSingleOrDefaultAsync<Order>();
        var items = await multi.ReadAsync<OrderItem>();
        return (header, items);
    }

    public async Task<int> PlaceAsync(int userId, int addressId, string paymentMethod,
        decimal totalAmount, decimal discountAmount, decimal shippingFee, decimal finalAmount,
        string? notes, object[] items)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@UserId", userId);
        p.Add("@AddressId", addressId);
        p.Add("@PaymentMethod", paymentMethod);
        p.Add("@TotalAmount", totalAmount);
        p.Add("@DiscountAmount", discountAmount);
        p.Add("@ShippingFee", shippingFee);
        p.Add("@FinalAmount", finalAmount);
        p.Add("@Notes", notes);
        p.Add("@ItemsJson", JsonSerializer.Serialize(items));
        p.Add("@NewOrderId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        p.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        p.Add("@ErrorMessage", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Orders_Place", p, commandType: CommandType.StoredProcedure);

        var success = p.Get<bool>("@Success");
        if (!success)
            throw new InvalidOperationException(p.Get<string?>("@ErrorMessage") ?? "Failed to place order.");

        return p.Get<int>("@NewOrderId");
    }

    public async Task<(bool Success, string? Error)> CancelAsync(int orderId, int userId, string? cancelReason)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@OrderId", orderId);
        p.Add("@UserId", userId);
        p.Add("@CancelReason", cancelReason);
        p.Add("@Success", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        p.Add("@ErrorMessage", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);

        await conn.ExecuteAsync("sp_Orders_Cancel", p, commandType: CommandType.StoredProcedure);
        return (p.Get<bool>("@Success"), p.Get<string?>("@ErrorMessage"));
    }

    public async Task<(IEnumerable<Order> Items, int Total)> GetAllAsync(string? orderStatus, string? paymentStatus,
        DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        using var conn = _db.Create();
        var p = new DynamicParameters();
        p.Add("@OrderStatus", orderStatus);
        p.Add("@PaymentStatus", paymentStatus);
        p.Add("@FromDate", fromDate);
        p.Add("@ToDate", toDate);
        p.Add("@Page", page);
        p.Add("@PageSize", pageSize);
        p.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var items = await conn.QueryAsync<Order>("sp_Orders_GetAll", p, commandType: CommandType.StoredProcedure);
        return (items, p.Get<int>("@TotalCount"));
    }

    public async Task UpdateStatusAsync(int orderId, string? orderStatus, string? paymentStatus,
        string? shiprocketOrderId, string? awbCode)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("sp_Orders_UpdateStatus", new
        {
            OrderId = orderId,
            OrderStatus = orderStatus,
            PaymentStatus = paymentStatus,
            ShiprocketOrderId = shiprocketOrderId,
            AWBCode = awbCode
        }, commandType: CommandType.StoredProcedure);
    }

    public async Task SaveRazorpayPaymentIdAsync(int orderId, string paymentId)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "UPDATE Orders SET RazorpayPaymentId = @PaymentId WHERE Id = @OrderId",
            new { OrderId = orderId, PaymentId = paymentId },
            commandType: CommandType.Text);
    }

    public async Task<bool> HasPurchasedAsync(int userId, int productId)
    {
        using var conn = _db.Create();
        var count = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1) FROM OrderItems oi
              JOIN Orders o ON o.Id = oi.OrderId
              WHERE o.UserId = @UserId AND oi.ProductId = @ProductId
              AND o.PaymentStatus = 'Paid'",
            new { UserId = userId, ProductId = productId },
            commandType: CommandType.Text);
        return count > 0;
    }
}
