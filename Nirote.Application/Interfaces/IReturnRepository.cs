using Nirote.Domain.Entities;

namespace Nirote.Application.Interfaces;

public interface IReturnRepository
{
    Task<int> CreateAsync(ReturnRequest request);
    Task<ReturnRequest?> GetByIdAsync(int id);
    Task<IEnumerable<ReturnRequest>> GetByUserAsync(int userId);
    Task<ReturnRequest?> GetByOrderAsync(int orderId);
    Task<IEnumerable<ReturnRequest>> GetAllAsync();
    Task UpdateStatusAsync(int id, string status, string? adminNote);
    Task UpdateRefundAsync(int id, string refundStatus, string? refundId);
    Task<bool> ExistsForOrderAsync(int orderId);
}
