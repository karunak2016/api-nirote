using Nirote.Application.DTOs.Payment;

namespace Nirote.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentOrderResponseDto> CreateRazorpayOrderAsync(int orderId);
    Task<bool> VerifyPaymentAsync(VerifyPaymentDto dto);
}
