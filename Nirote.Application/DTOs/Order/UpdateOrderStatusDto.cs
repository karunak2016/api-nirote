namespace Nirote.Application.DTOs.Order;

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? AwbCode { get; set; }
    public string? PaymentStatus { get; set; }
}
