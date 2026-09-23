using Nirote.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Razorpay.Api;

namespace Nirote.Infrastructure.ExternalServices;

public class RazorpayService : IRazorpayClient
{
    private readonly RazorpayClient _client;
    private readonly ILogger<RazorpayService> _logger;

    public RazorpayService(IConfiguration config, ILogger<RazorpayService> logger)
    {
        _client = new RazorpayClient(config["Razorpay:KeyId"], config["Razorpay:KeySecret"]);
        _logger = logger;
    }

    public Task<string> CreateOrderAsync(long amountInPaise, string currency, string receipt)
    {
        try
        {
            _logger.LogInformation("Creating Razorpay order amount={Amount} currency={Currency} receipt={Receipt}",
                amountInPaise, currency, receipt);

            if (amountInPaise <= 0)
                throw new InvalidOperationException($"Invalid order amount: {amountInPaise} paise. Amount must be greater than zero.");

            var options = new Dictionary<string, object>
            {
                ["amount"] = amountInPaise,
                ["currency"] = currency,
                ["receipt"] = receipt
            };
            var order = _client.Order.Create(options);
            var orderId = (string)order["id"];
            _logger.LogInformation("Razorpay order created: {RazorpayOrderId}", orderId);
            return Task.FromResult(orderId);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay CreateOrder failed for receipt={Receipt} — {Message}", receipt, ex.Message);
            throw new InvalidOperationException($"Payment gateway error: {ex.Message}");
        }
    }

    public Task<string> RefundAsync(string paymentId, long amountInPaise)
    {
        try
        {
            _logger.LogInformation("Issuing Razorpay refund for payment {PaymentId} amount {Amount}", paymentId, amountInPaise);
            var options = new Dictionary<string, object>
            {
                ["amount"] = amountInPaise,
                ["speed"] = "normal"
            };
            var refund = _client.Payment.Fetch(paymentId).Refund(options);
            var refundId = (string)refund["id"];
            _logger.LogInformation("Razorpay refund {RefundId} issued for payment {PaymentId}", refundId, paymentId);
            return Task.FromResult(refundId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay refund failed for payment {PaymentId}", paymentId);
            throw;
        }
    }
}
