namespace Nirote.Application.DTOs.Order;

public class PlaceOrderDto
{
    public int AddressId { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? CouponCode { get; set; }
    public decimal ShippingFee { get; set; }
}
