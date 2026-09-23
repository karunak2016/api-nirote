using System.Security.Cryptography;
using System.Text;
using Nirote.Application.DTOs.Payment;
using Nirote.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nirote.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _config;
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IRazorpayClient _razorpay;
    private readonly IOrderService _orderService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IConfiguration config, IOrderRepository orderRepo, ICartRepository cartRepo, IRazorpayClient razorpay, IOrderService orderService, ILogger<PaymentService> logger)
    {
        _config = config;
        _orderRepo = orderRepo;
        _cartRepo = cartRepo;
        _razorpay = razorpay;
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<PaymentOrderResponseDto> CreateRazorpayOrderAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Create Razorpay order for orderId {OrderId}", orderId);
            var (order, _) = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new KeyNotFoundException("Order not found.");

            var amountInPaise = (long)(order.FinalAmount * 100);
            var razorpayOrderId = await _razorpay.CreateOrderAsync(amountInPaise, "INR", orderId.ToString());

            _logger.LogInformation("Razorpay order {RazorpayOrderId} created for orderId {OrderId}", razorpayOrderId, orderId);
            return new PaymentOrderResponseDto
            {
                RazorpayOrderId = razorpayOrderId,
                Amount = amountInPaise,
                Currency = "INR",
                KeyId = _config["Razorpay:KeyId"]!
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create Razorpay order failed for orderId {OrderId}", orderId);
            throw;
        }
    }

    public async Task<bool> VerifyPaymentAsync(VerifyPaymentDto dto)
    {
        try
        {
            _logger.LogInformation("Verify payment for orderId {OrderId}", dto.OrderId);
            var secret = _config["Razorpay:KeySecret"]!;
            var payload = $"{dto.RazorpayOrderId}|{dto.RazorpayPaymentId}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computed = BitConverter.ToString(hash).Replace("-", "").ToLower();

            if (computed != dto.RazorpaySignature)
            {
                _logger.LogWarning("Payment signature mismatch for orderId {OrderId}", dto.OrderId);
                return false;
            }

            await _orderRepo.UpdateStatusAsync(dto.OrderId, "Confirmed", "Paid", null, null);
            if (!string.IsNullOrWhiteSpace(dto.RazorpayPaymentId))
                await _orderRepo.SaveRazorpayPaymentIdAsync(dto.OrderId, dto.RazorpayPaymentId);
            var (order, _) = await _orderRepo.GetByIdAsync(dto.OrderId);
            if (order != null) await _cartRepo.ClearAsync(order.UserId);
            await _orderService.SendConfirmationEmailAsync(dto.OrderId);
            _logger.LogInformation("Payment verified and order {OrderId} confirmed", dto.OrderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify payment failed for orderId {OrderId}", dto.OrderId);
            throw;
        }
    }
}

public interface IRazorpayClient
{
    Task<string> CreateOrderAsync(long amountInPaise, string currency, string receipt);
    Task<string> RefundAsync(string paymentId, long amountInPaise);
}
