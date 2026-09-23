using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Nirote.Application.Interfaces;
using Nirote.Application.Services;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Data;
using Nirote.Infrastructure.ExternalServices;
using Nirote.Infrastructure.Repositories;
using Nirote.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Nirote.API.Extensions;

public static class ServiceExtensions
{
    // Cache Google's public certs (refreshed every hour)
    private static Dictionary<string, X509Certificate2>? _googleCerts;
    private static DateTime _certsExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _certLock = new(1, 1);

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwt["SecretKey"]!);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowClients", policy =>
                policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!)
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<BlobStorageService>();
        services.AddScoped<IProductOptionsRepository, ProductOptionsRepository>();

        // Repositories
        services.AddScoped<UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<AddressRepository>();

        // External services
        services.AddScoped<IRazorpayClient, RazorpayService>();
        services.AddHttpClient<IShiprocketClient, ShiprocketService>();

        // Application services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReturnRepository, ReturnRepository>();

        services.AddMemoryCache();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        // Stage 1  - " mock (OTP = 123456, no DLT needed):
        services.AddScoped<ISmsService, MockSmsService>();
        // Stage 2+  - " Fast2SMS (enable after DLT registration):
        // services.AddHttpClient<ISmsService, Fast2SmsService>();

        services.AddScoped<IAuthService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var hasher = sp.GetRequiredService<IPasswordHasher<User>>();
            var repo = sp.GetRequiredService<UserRepository>();
            var logger = sp.GetRequiredService<ILogger<AuthService>>();
            var sms = sp.GetRequiredService<ISmsService>();
            var projectId = config["Firebase:ProjectId"] ?? "";
            return new AuthService(
                config, hasher, logger, sms,
                idToken => VerifyFirebaseTokenAsync(idToken, projectId),
                () => Task.FromResult(new List<User>()),
                repo.GetByEmailAsync,
                repo.GetByPhoneAsync,
                async u => { await repo.CreateAsync(u); });
        });

        return services;
    }

    private static async Task<string?> VerifyFirebaseTokenAsync(string idToken, string projectId)
    {
        var certs = await GetGoogleCertsAsync();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        if (!jwt.Header.TryGetValue("kid", out var kidObj) || kidObj?.ToString() is not string kid
            || !certs.TryGetValue(kid, out var cert))
            throw new UnauthorizedAccessException("Invalid Firebase token.");

        handler.ValidateToken(idToken, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new X509SecurityKey(cert),
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        }, out var validatedToken);

        var claims = ((JwtSecurityToken)validatedToken).Claims;
        return claims.FirstOrDefault(c => c.Type == "phone_number")?.Value;
    }

    private static async Task<Dictionary<string, X509Certificate2>> GetGoogleCertsAsync()
    {
        if (_googleCerts != null && DateTime.UtcNow < _certsExpiry)
            return _googleCerts;

        await _certLock.WaitAsync();
        try
        {
            if (_googleCerts != null && DateTime.UtcNow < _certsExpiry)
                return _googleCerts;

            using var http = new HttpClient();
            var json = await http.GetStringAsync(
                "https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com");

            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
            _googleCerts = raw.ToDictionary(
                kvp => kvp.Key,
                kvp => X509Certificate2.CreateFromPem(kvp.Value));
            _certsExpiry = DateTime.UtcNow.AddHours(1);

            return _googleCerts;
        }
        finally
        {
            _certLock.Release();
        }
    }
}
