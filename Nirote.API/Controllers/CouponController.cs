using Nirote.Application.DTOs.Coupon;
using Nirote.Application.Interfaces;
using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/coupons")]
public class CouponController(
    ICouponService coupons,
    UserRepository users,
    IEmailService email,
    ILogger<CouponController> logger) : ControllerBase
{
    // â"€â"€ Customer endpoints â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateCouponDto dto)
    {
        try
        {
            var result = await coupons.ValidateAsync(dto.Code, dto.CartTotal);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Coupon validate failed");
            throw;
        }
    }

    [HttpGet("active-offers")]
    public async Task<IActionResult> GetActiveOffers()
    {
        try
        {
            var offers = await coupons.GetActiveAutoOffersAsync();
            return Ok(offers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get active offers failed");
            throw;
        }
    }

    [HttpGet("bank-offers")]
    public async Task<IActionResult> GetBankOffers()
    {
        try
        {
            var offers = await coupons.GetBankOffersAsync();
            return Ok(offers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get bank offers failed");
            throw;
        }
    }

    // â"€â"€ Admin endpoints â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€â"€

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await coupons.GetAllAsync());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get all coupons failed");
            throw;
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCouponDto dto)
    {
        try
        {
            var result = await coupons.CreateAsync(dto);
            logger.LogInformation("Coupon created: {Code}", result.Code);
            return CreatedAtAction(nameof(GetAll), result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create coupon failed");
            throw;
        }
    }

    [HttpPost("update/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponDto dto)
    {
        try
        {
            var result = await coupons.UpdateAsync(id, dto, dto.IsActive);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update coupon {Id} failed", id);
            throw;
        }
    }

    [HttpPost("delete/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var all = await coupons.GetAllAsync();
            var existing = all.FirstOrDefault(c => c.Id == id);
            if (existing == null) return NotFound();
            await coupons.UpdateAsync(id, new UpdateCouponDto
            {
                Code = existing.Code, Description = existing.Description,
                DiscountType = existing.DiscountType, DiscountValue = existing.DiscountValue,
                MinCartAmount = existing.MinCartAmount, MaxDiscount = existing.MaxDiscount,
                StartDate = existing.StartDate, EndDate = existing.EndDate,
                UsageLimit = existing.UsageLimit, FestivalName = existing.FestivalName,
                BankName = existing.BankName, IsActive = false,
            }, false);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deactivate coupon {Id} failed", id);
            throw;
        }
    }

    [HttpPost("{id:int}/send-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendEmail(int id)
    {
        try
        {
            var all = await coupons.GetAllAsync();
            var coupon = all.FirstOrDefault(c => c.Id == id);
            if (coupon == null) return NotFound();

            var emails = (await users.GetAllCustomerEmailsAsync()).ToList();
            if (emails.Count == 0) return Ok(new { sent = 0, message = "No customers found." });

            var subject = BuildSubject(coupon);
            var body = BuildEmailBody(coupon);
            await email.SendBulkAsync(emails, subject, body);

            logger.LogInformation("Coupon email sent for {Code} to {Count} customers", coupon.Code, emails.Count);
            return Ok(new { sent = emails.Count });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Send coupon email failed for coupon {Id}", id);
            throw;
        }
    }

    private static string BuildSubject(CouponDto c)
    {
        if (!string.IsNullOrEmpty(c.FestivalName))
            return $"{c.FestivalName} Special Offer - Nirote";
        return c.DiscountType == "Percentage"
            ? $"Get {c.DiscountValue}% Off - Exclusive Offer from Nirote"
            : $"Get Rs.{c.DiscountValue} Off - Exclusive Offer from Nirote";
    }

    private static string BuildEmailBody(CouponDto c) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;background:#f9fafb;">
          <div style="background:white;border-radius:12px;padding:32px;border:1px solid #e5e7eb;">
            <h1 style="color:#c9a84c;font-size:22px;margin:0 0 4px;">Niroté</h1>
            <p style="color:#9ca3af;font-size:13px;margin:0 0 24px;">Handcrafted jewellery, delivered to your door.</p>

            <h2 style="color:#1f2937;font-size:18px;margin:0 0 16px;">{c.Description}</h2>

            {(c.Code != null ? $"""
            <div style="background:#fef3c7;border:2px dashed #d97706;border-radius:8px;padding:20px;text-align:center;margin:20px 0;">
              <p style="color:#92400e;font-size:11px;margin:0 0 8px;text-transform:uppercase;letter-spacing:1px;">Use code at checkout</p>
              <span style="font-size:30px;font-weight:bold;color:#92400e;font-family:monospace;letter-spacing:4px;">{c.Code}</span>
            </div>
            """ : "")}

            <ul style="color:#374151;line-height:2.2;padding-left:20px;">
              <li><strong>{(c.DiscountType == "Percentage" ? $"{c.DiscountValue}% off" : $"Rs.{c.DiscountValue} off")}</strong> on your order{(c.MaxDiscount.HasValue ? $" (max Rs.{c.MaxDiscount})" : "")}</li>
              {(c.MinCartAmount.HasValue ? $"<li>Minimum order: Rs.{c.MinCartAmount}</li>" : "")}
              {(c.EndDate.HasValue ? $"<li>Valid until: <strong>{c.EndDate.Value:dd MMM yyyy}</strong></li>" : "<li>No expiry - use anytime!</li>")}
            </ul>

            <a href="https://Nirote.com/products"
               style="display:inline-block;background:#6b2f1a;color:white;padding:13px 30px;border-radius:8px;text-decoration:none;font-weight:bold;margin-top:16px;font-size:15px;">
              Shop Now
            </a>

            <hr style="border:none;border-top:1px solid #e5e7eb;margin:28px 0 16px;" />
            <p style="color:#9ca3af;font-size:11px;margin:0;">
              You received this email because you have an account with Niroté.<br/>
              &copy; {DateTime.UtcNow.Year} Niroté. All rights reserved.
            </p>
          </div>
        </body>
        </html>
        """;
}
