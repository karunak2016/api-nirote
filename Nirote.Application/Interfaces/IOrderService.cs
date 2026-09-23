using Nirote.Application.DTOs.Order;
using Nirote.Application.DTOs.Product;

namespace Nirote.Application.Interfaces;

public interface IOrderService
{
    Task<List<OrderDto>> GetCustomerOrdersAsync(int userId);
    Task<OrderDto?> GetByIdAsync(int orderId, int userId);
    Task<OrderDto?> GetByIdAdminAsync(int orderId);
    Task<OrderDto> PlaceOrderAsync(int userId, PlaceOrderDto dto);
    Task CancelOrderAsync(int orderId, int userId, string? reason = null);
    Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page, int pageSize);
    Task UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto);
    Task SendConfirmationEmailAsync(int orderId);
}
