using Nirote.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Nirote.Application.Services;

public class ShippingService : IShippingService
{
    private readonly IShiprocketClient _shiprocket;
    private readonly IOrderRepository _orderRepo;
    private readonly ILogger<ShippingService> _logger;

    public ShippingService(IShiprocketClient shiprocket, IOrderRepository orderRepo, ILogger<ShippingService> logger)
    {
        _shiprocket = shiprocket;
        _orderRepo = orderRepo;
        _logger = logger;
    }

    public async Task<bool> CheckServiceabilityAsync(string pincode)
    {
        try
        {
            _logger.LogInformation("Check serviceability for pincode {Pincode}", pincode);
            return await _shiprocket.CheckServiceabilityAsync(pincode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check serviceability failed for pincode {Pincode}", pincode);
            throw;
        }
    }

    public async Task<string> CreateShipmentAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Create shipment for order {OrderId}", orderId);
            var (order, items) = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new KeyNotFoundException("Order not found.");

            var awbCode = await _shiprocket.CreateShipmentAsync(order, items);
            await _orderRepo.UpdateStatusAsync(orderId, "Shipped", null, null, awbCode);
            _logger.LogInformation("Shipment created for order {OrderId} AWB={AWBCode}", orderId, awbCode);
            return awbCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create shipment failed for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<object> TrackShipmentAsync(string awbCode)
    {
        try
        {
            _logger.LogInformation("Track shipment AWB={AWBCode}", awbCode);
            return await _shiprocket.TrackAsync(awbCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Track shipment failed for AWB={AWBCode}", awbCode);
            throw;
        }
    }
}

public interface IShiprocketClient
{
    Task<bool> CheckServiceabilityAsync(string pincode);
    Task<string> CreateShipmentAsync(Nirote.Domain.Entities.Order order, IEnumerable<Nirote.Domain.Entities.OrderItem> items);
    Task<object> TrackAsync(string awbCode);
}
