using Nirote.Application.DTOs.Auth;

namespace Nirote.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
    Task<AuthResponseDto> AdminLoginAsync(LoginRequestDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string token);
    Task SendOtpAsync(OtpSendRequestDto dto);
    Task<AuthResponseDto> VerifyOtpAsync(OtpVerifyRequestDto dto);
    Task<AuthResponseDto> LoginWithFirebaseAsync(FirebaseLoginRequestDto dto);
}
