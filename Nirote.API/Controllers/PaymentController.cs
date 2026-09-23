using Nirote.Application.DTOs.Payment;
using Nirote.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/payment")]
[Authorize(Roles = "Customer,Admin")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payment;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService payment, ILogger<PaymentController> logger)
    {
        _payment = payment;
        _logger = logger;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder(CreatePaymentOrderDto dto)
    {
        try
        {
            _logger.LogInformation("Create Razorpay order for orderId {OrderId}", dto.OrderId);
            var result = await _payment.CreateRazorpayOrderAsync(dto.OrderId);
            _logger.LogInformation("Razorpay order created for orderId {OrderId}", dto.OrderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create Razorpay order failed for orderId {OrderId}", dto.OrderId);
            throw;
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyPaymentDto dto)
    {
        try
        {
            _logger.LogInformation("Verify payment for orderId {OrderId}", dto.OrderId);
            var success = await _payment.VerifyPaymentAsync(dto);
            if (success)
                _logger.LogInformation("Payment verified for orderId {OrderId}", dto.OrderId);
            else
                _logger.LogWarning("Payment verification failed for orderId {OrderId}", dto.OrderId);
            return success ? Ok(new { success = true }) : BadRequest(new { success = false, message = "Invalid payment signature." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify payment threw exception for orderId {OrderId}", dto.OrderId);
            throw;
        }
    }
}
