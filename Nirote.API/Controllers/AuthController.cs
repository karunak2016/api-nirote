using System.Security.Claims;
using Nirote.Application.DTOs.Auth;
using Nirote.Application.Interfaces;
using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;
    private readonly UserRepository _users;

    public AuthController(IAuthService auth, ILogger<AuthController> logger, UserRepository users)
    {
        _auth = auth;
        _logger = logger;
        _users = users;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Register attempt for {Email}", dto.Email);
            var result = await _auth.RegisterAsync(dto);
            _logger.LogInformation("User registered: {Email}", dto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register failed for {Email}", dto.Email);
            throw;
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Login attempt for {Email}", dto.Email);
            var result = await _auth.LoginAsync(dto);
            _logger.LogInformation("Login successful for {Email}", dto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", dto.Email);
            throw;
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string token)
    {
        try
        {
            _logger.LogInformation("Token refresh requested");
            var result = await _auth.RefreshTokenAsync(token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            throw;
        }
    }

    [HttpPost("firebase")]
    public async Task<IActionResult> FirebaseLogin(FirebaseLoginRequestDto dto)
    {
        var result = await _auth.LoginWithFirebaseAsync(dto);
        return Ok(result);
    }

    [HttpPost("otp/send")]
    public async Task<IActionResult> SendOtp(OtpSendRequestDto dto)
    {
        await _auth.SendOtpAsync(dto);
        return Ok(new { message = "OTP sent." });
    }

    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp(OtpVerifyRequestDto dto)
    {
        var result = await _auth.VerifyOtpAsync(dto);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("profile/update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _users.UpdateProfileAsync(userId, dto.Name, dto.Email, dto.Phone);
        return Ok(new { message = "Profile updated." });
    }

    [Authorize]
    [HttpPost("profile/set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _users.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Nirote.Domain.Entities.User>();
        var hash = hasher.HashPassword(user, dto.NewPassword);
        await _users.SetPasswordAsync(userId, hash);
        return Ok(new { message = "Password set successfully. You can now login with your email and password." });
    }

    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin(LoginRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Admin login attempt for {Email}", dto.Email);
            var result = await _auth.AdminLoginAsync(dto);
            _logger.LogInformation("Admin login successful for {Email}", dto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin login failed for {Email}", dto.Email);
            throw;
        }
    }
}
