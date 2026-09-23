using System.Net.Http.Json;
using System.Text.Json;
using Nirote.Application.Services;
using Nirote.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nirote.Infrastructure.ExternalServices;

public class ShiprocketService : IShiprocketClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ShiprocketService> _logger;
    private string? _token;

    public ShiprocketService(HttpClient http, IConfiguration config, ILogger<ShiprocketService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _http.BaseAddress = new Uri(config["Shiprocket:BaseUrl"]!);
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_token != null) return;

        try
        {
            _logger.LogInformation("Authenticating with Shiprocket");
            var response = await _http.PostAsJsonAsync("/auth/login", new
            {
                email = _config["Shiprocket:Email"],
                password = _config["Shiprocket:Password"]
            });
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            _token = json.GetProperty("token").GetString();
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            _logger.LogInformation("Shiprocket authentication successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket authentication failed");
            throw;
        }
    }

    public async Task<bool> CheckServiceabilityAsync(string pincode)
    {
        try
        {
            _logger.LogInformation("Shiprocket check serviceability for pincode {Pincode}", pincode);
            await EnsureAuthenticatedAsync();
            var response = await _http.GetAsync(
                $"/courier/serviceability/?pickup_postcode=110001&delivery_postcode={pincode}&cod=0&weight=1");
            var serviceable = response.IsSuccessStatusCode;
            _logger.LogInformation("Shiprocket serviceability for {Pincode}: {Result}", pincode, serviceable);
            return serviceable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket check serviceability failed for pincode {Pincode}", pincode);
            throw;
        }
    }

    public async Task<string> CreateShipmentAsync(Order order, IEnumerable<OrderItem> items)
    {
        try
        {
            _logger.LogInformation("Shiprocket create shipment for order {OrderId}", order.Id);
            await EnsureAuthenticatedAsync();

            var nameParts = (order.DeliveryName ?? order.CustomerName ?? "Customer").Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : ".";
            var phone = (order.DeliveryPhone ?? order.CustomerPhone ?? "9999999999")
                        .Replace(" ", "").Replace("-", "");

            var payload = new
            {
                order_id = order.Id.ToString(),
                order_date = order.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                pickup_location = "Primary",
                payment_method = order.PaymentMethod == "COD" ? "COD" : "Prepaid",
                billing_customer_name = firstName,
                billing_last_name = lastName,
                billing_address = order.AddressLine1 ?? "N/A",
                billing_address_2 = order.AddressLine2 ?? "",
                billing_city = order.City ?? "N/A",
                billing_pincode = order.Pincode ?? "000000",
                billing_state = order.State ?? "N/A",
                billing_country = "India",
                billing_email = order.CustomerEmail ?? "",
                billing_phone = phone,
                shipping_is_billing = 1,
                order_items = items.Select(i => new
                {
                    name = i.ProductName,
                    sku = $"SKU-{i.ProductId}",
                    units = i.Quantity,
                    selling_price = (double)i.UnitPrice,
                    discount = 0,
                    tax = 0
                }).ToArray(),
                sub_total = (double)order.FinalAmount,
                length = 30.0,
                breadth = 20.0,
                height = 5.0,
                weight = 0.5
            };

            var response = await _http.PostAsJsonAsync("/orders/create/adhoc", payload);
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Shiprocket create order response: {Body}", body);
            response.EnsureSuccessStatusCode();

            var json = JsonSerializer.Deserialize<JsonElement>(body);

            string? awbCode = null;
            if (json.TryGetProperty("awb_code", out var awbEl) && awbEl.ValueKind != JsonValueKind.Null)
                awbCode = awbEl.GetString();

            if (string.IsNullOrEmpty(awbCode) && json.TryGetProperty("shipment_id", out var shipEl))
            {
                var shipmentId = shipEl.GetInt64();
                awbCode = await AssignCourierAsync(shipmentId);
            }

            _logger.LogInformation("Shiprocket shipment created AWB={AWBCode} for order {OrderId}", awbCode, order.Id);
            return awbCode ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket create shipment failed for order {OrderId}", order.Id);
            throw;
        }
    }

    private async Task<string> AssignCourierAsync(long shipmentId)
    {
        try
        {
            _logger.LogInformation("Shiprocket assign courier for shipment {ShipmentId}", shipmentId);
            var response = await _http.PostAsJsonAsync("/courier/assign/awb",
                new { shipment_id = shipmentId.ToString() });
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Shiprocket assign courier response: {Body}", body);
            response.EnsureSuccessStatusCode();
            var json = JsonSerializer.Deserialize<JsonElement>(body);
            return json.TryGetProperty("awb_code", out var el) ? el.GetString() ?? "" : "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket assign courier failed for shipment {ShipmentId}", shipmentId);
            throw;
        }
    }

    public async Task<object> TrackAsync(string awbCode)
    {
        try
        {
            _logger.LogInformation("Shiprocket track AWB={AWBCode}", awbCode);
            await EnsureAuthenticatedAsync();
            var response = await _http.GetAsync($"/courier/track/awb/{awbCode}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>() ?? new { };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shiprocket track failed for AWB={AWBCode}", awbCode);
            throw;
        }
    }
}
