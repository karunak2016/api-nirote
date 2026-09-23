namespace Nirote.Application.DTOs.Payment;

public class CreatePaymentOrderDto
{
    public int OrderId { get; set; }
}

public class PaymentOrderResponseDto
{
    public string RazorpayOrderId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string KeyId { get; set; } = string.Empty;
}
