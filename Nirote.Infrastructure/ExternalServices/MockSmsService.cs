using Nirote.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Nirote.Infrastructure.ExternalServices;

public class MockSmsService : ISmsService
{
    private const string FixedOtp = "123456";
    private readonly ILogger<MockSmsService> _logger;

    public MockSmsService(ILogger<MockSmsService> logger) => _logger = logger;

    public Task SendOtpAsync(string phone)
    {
        _logger.LogInformation("MockSms: OTP for {Phone} is {Otp}", phone, FixedOtp);
        return Task.CompletedTask;
    }

    public Task VerifyOtpAsync(string phone, string otp)
    {
        if (otp != FixedOtp)
            throw new UnauthorizedAccessException("Invalid OTP.");
        return Task.CompletedTask;
    }
}
