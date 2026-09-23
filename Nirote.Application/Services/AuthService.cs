using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Nirote.Application.DTOs.Auth;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Nirote.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Nirote.Application.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILogger<AuthService> _logger;
    private readonly ISmsService _sms;
    private readonly Func<string, Task<string?>> _verifyFirebaseToken;
    private readonly Func<Task<List<User>>> _getAllUsers;
    private readonly Func<string, Task<User?>> _getUserByEmail;
    private readonly Func<string, Task<User?>> _getUserByPhone;
    private readonly Func<User, Task> _createUser;

    public AuthService(
        IConfiguration config,
        IPasswordHasher<User> hasher,
        ILogger<AuthService> logger,
        ISmsService sms,
        Func<string, Task<string?>> verifyFirebaseToken,
        Func<Task<List<User>>> getAllUsers,
        Func<string, Task<User?>> getUserByEmail,
        Func<string, Task<User?>> getUserByPhone,
        Func<User, Task> createUser)
    {
        _config = config;
        _hasher = hasher;
        _logger = logger;
        _sms = sms;
        _verifyFirebaseToken = verifyFirebaseToken;
        _getAllUsers = getAllUsers;
        _getUserByEmail = getUserByEmail;
        _getUserByPhone = getUserByPhone;
        _createUser = createUser;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Registering user {Email}", dto.Email);
            var existing = await _getUserByEmail(dto.Email);
            if (existing != null) throw new InvalidOperationException("Email already registered.");

            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var existingPhone = await _getUserByPhone(dto.Phone);
                if (existingPhone != null) throw new InvalidOperationException("Mobile number already registered with another account.");
            }

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone,
                Role = UserRole.Customer
            };
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            await _createUser(user);
            _logger.LogInformation("User {Email} registered successfully", dto.Email);
            return BuildToken(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register failed for {Email}", dto.Email);
            throw;
        }
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Login attempt for {Email}", dto.Email);
            var user = await _getUserByEmail(dto.Email)
                ?? throw new UnauthorizedAccessException("Invalid credentials.");

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid credentials.");

            _logger.LogInformation("User {Email} logged in", dto.Email);
            return BuildToken(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {Email}", dto.Email);
            throw;
        }
    }

    public async Task<AuthResponseDto> AdminLoginAsync(LoginRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Admin login attempt for {Email}", dto.Email);
            var user = await _getUserByEmail(dto.Email)
                ?? throw new UnauthorizedAccessException("Invalid credentials.");

            if (user.Role != UserRole.Admin)
                throw new UnauthorizedAccessException("Not an admin account.");

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid credentials.");

            _logger.LogInformation("Admin {Email} logged in", dto.Email);
            return BuildToken(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin login failed for {Email}", dto.Email);
            throw;
        }
    }

    public Task<AuthResponseDto> RefreshTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Refreshing token");
            var principal = GetPrincipalFromToken(token);
            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? throw new UnauthorizedAccessException("Invalid token.");

            return _getUserByEmail(email).ContinueWith(t =>
            {
                var user = t.Result ?? throw new UnauthorizedAccessException("User not found.");
                _logger.LogInformation("Token refreshed for {Email}", email);
                return BuildToken(user);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            throw;
        }
    }

    public async Task<AuthResponseDto> LoginWithFirebaseAsync(FirebaseLoginRequestDto dto)
    {
        var phone = await _verifyFirebaseToken(dto.IdToken)
            ?? throw new UnauthorizedAccessException("Invalid Firebase token.");

        // Normalize: strip +91 country code if present
        if (phone.StartsWith("+91")) phone = phone[3..];

        var email = $"{phone}@nirote.local";
        var user = await _getUserByEmail(email)
            ?? await _getUserByPhone(phone);  // find existing account by phone
        if (user == null)
        {
            user = new User { Name = "Customer", Email = email, Phone = phone, Role = UserRole.Customer };
            user.PasswordHash = _hasher.HashPassword(user, Guid.NewGuid().ToString());
            await _createUser(user);
            user = await _getUserByEmail(email) ?? user;
        }

        return BuildToken(user);
    }

    public Task SendOtpAsync(OtpSendRequestDto dto) => _sms.SendOtpAsync(dto.Phone);

    public async Task<AuthResponseDto> VerifyOtpAsync(OtpVerifyRequestDto dto)
    {
        await _sms.VerifyOtpAsync(dto.Phone, dto.Otp);

        var email = $"{dto.Phone}@nirote.local";
        var user = await _getUserByEmail(email)
            ?? await _getUserByPhone(dto.Phone);  // find existing account by phone
        if (user == null)
        {
            user = new User { Name = "Customer", Email = email, Phone = dto.Phone, Role = UserRole.Customer };
            user.PasswordHash = _hasher.HashPassword(user, Guid.NewGuid().ToString());
            await _createUser(user);
            user = await _getUserByEmail(email) ?? user;
        }

        return BuildToken(user);
    }

    private AuthResponseDto BuildToken(User user)
    {
        var jwt = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiryMinutes"]!));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponseDto
        {
            UserId = user.Id,
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            ExpiresAt = expiry
        };
    }

    private ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var jwt = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false
        }, out _);
    }
}
