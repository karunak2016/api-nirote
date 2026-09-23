namespace Nirote.Application.DTOs.Auth;

public class OtpVerifyRequestDto
{
    public string Phone { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}
