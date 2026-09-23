using System.Text.Json;
using Nirote.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nirote.Infrastructure.ExternalServices;

public class Fast2SmsService : ISmsService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly ILogger<Fast2SmsService> _logger;

    public Fast2SmsService(HttpClient http, IConfiguration config, IMemoryCache cache, ILogger<Fast2SmsService> logger)
    {
        _http = http;
        _config = config;
        _cache = cache;
        _logger = logger;
    }

    public async Task SendOtpAsync(string phone)
    {
        var apiKey = _config["Fast2Sms:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("SMS service is not configured.");

        // Smart OTP — no DLT registration required
        var url = $"https://www.fast2sms.com/dev/smartotp?authorization={apiKey}&number={phone}";

        var response = await _http.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Fast2SMS SmartOTP response for {Phone}: {Body}", phone, body);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to send OTP. Please try again.");

        using var doc = JsonDocument.Parse(body);
        var success = doc.RootElement.TryGetProperty("return", out var ret) && ret.GetBoolean();
        if (!success)
        {
            var message = doc.RootElement.TryGetProperty("message", out var msg) ? msg.ToString() : body;
            _logger.LogWarning("Fast2SMS rejected SmartOTP for {Phone}: {Message}", phone, message);
            throw new InvalidOperationException("Failed to send OTP. Please try again.");
        }

        // Smart OTP returns the generated OTP — store it for verification
        if (doc.RootElement.TryGetProperty("otp", out var otpProp))
            _cache.Set($"otp:{phone}", otpProp.ToString(), TimeSpan.FromMinutes(10));
        else
            throw new InvalidOperationException("OTP not returned by SMS service.");
    }

    public Task VerifyOtpAsync(string phone, string otp)
    {
        if (!_cache.TryGetValue($"otp:{phone}", out string? stored) || stored != otp)
            throw new UnauthorizedAccessException("Invalid or expired OTP.");

        _cache.Remove($"otp:{phone}");
        return Task.CompletedTask;
    }
}
