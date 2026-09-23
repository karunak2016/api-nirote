namespace Nirote.Application.Interfaces;

public interface ISmsService
{
    Task SendOtpAsync(string phone);
    Task VerifyOtpAsync(string phone, string otp); // throws UnauthorizedAccessException on failure
}
