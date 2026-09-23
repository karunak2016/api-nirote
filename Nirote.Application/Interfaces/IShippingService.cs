namespace Nirote.Application.Interfaces;

public interface IShippingService
{
    Task<bool> CheckServiceabilityAsync(string pincode);
    Task<string> CreateShipmentAsync(int orderId);
    Task<object> TrackShipmentAsync(string awbCode);
}
