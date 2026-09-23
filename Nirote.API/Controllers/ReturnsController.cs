using System.Security.Claims;
using Nirote.Application.DTOs.Return;
using Nirote.Application.Interfaces;
using Nirote.Application.Services;
using Nirote.Domain.Entities;
using Nirote.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/returns")]
public class ReturnsController(
    IReturnRepository returns,
    IOrderRepository orders,
    IEmailService email,
    IRazorpayClient razorpay,
    UserRepository users,
    ILogger<ReturnsController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReturnRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { error = "Reason is required." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Customer";

            // Verify order belongs to user and is Delivered
            var (order, _) = await orders.GetByIdAsync(dto.OrderId);
            if (order == null || order.UserId != userId)
                return BadRequest(new { error = "Order not found." });
            if (order.OrderStatus != "Delivered")
                return BadRequest(new { error = "Only delivered orders can be returned." });
            if (order.PaymentStatus != "Paid")
                return BadRequest(new { error = "Only paid orders can be returned." });

            // Enforce 48-hour return window from delivery date
            if (order.DeliveredAt.HasValue && DateTime.UtcNow > order.DeliveredAt.Value.AddHours(48))
                return BadRequest(new { error = "Return window has expired. Returns must be requested within 48 hours of delivery." });

            // Check if return already requested
            var exists = await returns.ExistsForOrderAsync(dto.OrderId);
            if (exists)
                return BadRequest(new { error = "A return request already exists for this order." });

            var id = await returns.CreateAsync(new ReturnRequest
            {
                OrderId = dto.OrderId,
                UserId = userId,
                UserName = userName,
                Reason = dto.Reason,
                Description = dto.Description,
            });

            return Ok(new { id, message = "Return request submitted successfully. We will review it within 2-3 business days." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Create return request failed");
            throw;
        }
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var list = await returns.GetByUserAsync(userId);
            return Ok(list.Select(Map));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get my returns failed");
            throw;
        }
    }

    [HttpGet("order/{orderId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        try
        {
            var r = await returns.GetByOrderAsync(orderId);
            if (r == null) return Ok(null);
            return Ok(Map(r));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get return for order {OrderId} failed", orderId);
            throw;
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await returns.GetAllAsync();
            return Ok(list.Select(Map));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get all returns failed");
            throw;
        }
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReturnStatusDto dto)
    {
        try
        {
            if (dto.Status != "Approved" && dto.Status != "Rejected")
                return BadRequest(new { error = "Status must be Approved or Rejected." });

            await returns.UpdateStatusAsync(id, dto.Status, dto.AdminNote);

            // Send email notification to customer
            var r = await returns.GetByIdAsync(id);
            if (r != null)
            {
                var customerEmail = await users.GetEmailByUserIdAsync(r.UserId);
                if (!string.IsNullOrWhiteSpace(customerEmail))
                {
                    try
                    {
                        var subject = dto.Status == "Approved"
                            ? "Your Return Request Has Been Approved - Nirote"
                            : "Update on Your Return Request - Nirote";
                        var html = BuildReturnEmail(r, dto.Status, dto.AdminNote);
                        await email.SendAsync(customerEmail, subject, html);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to send return status email for return {Id}", id);
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update return status {Id} failed", id);
            throw;
        }
    }

    [HttpPost("{id:int}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IssueRefund(int id)
    {
        try
        {
            var r = await returns.GetByIdAsync(id);
            if (r == null) return NotFound();
            if (r.Status != "Approved")
                return BadRequest(new { error = "Return must be approved before issuing a refund." });
            if (r.RefundStatus == "Issued")
                return BadRequest(new { error = "Refund has already been issued." });

            var (order, _) = await orders.GetByIdAsync(r.OrderId);
            if (order == null) return NotFound(new { error = "Order not found." });

            string? refundId = null;
            var isCod = order.PaymentMethod?.Equals("COD", StringComparison.OrdinalIgnoreCase) == true;

            if (!isCod && !string.IsNullOrWhiteSpace(order.RazorpayPaymentId))
            {
                await returns.UpdateRefundAsync(id, "Processing", null);
                try
                {
                    var amountInPaise = (long)(order.FinalAmount * 100);
                    refundId = await razorpay.RefundAsync(order.RazorpayPaymentId, amountInPaise);
                    await returns.UpdateRefundAsync(id, "Issued", refundId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Razorpay refund failed for return {Id}", id);
                    await returns.UpdateRefundAsync(id, "Failed", null);
                    return StatusCode(500, new { error = "Refund processing failed. Please try again or process manually." });
                }
            }
            else
            {
                await returns.UpdateRefundAsync(id, "Issued", null);
            }

            try
            {
                var customerEmail = await users.GetEmailByUserIdAsync(r.UserId);
                if (!string.IsNullOrWhiteSpace(customerEmail))
                {
                    var html = BuildRefundEmail(r, order.FinalAmount, isCod, refundId);
                    await email.SendAsync(customerEmail, "Your Refund Has Been Processed - Nirote", html);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send refund email for return {Id}", id);
            }

            return Ok(new { message = "Refund issued successfully.", refundId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Issue refund for return {Id} failed", id);
            throw;
        }
    }

    private static string BuildRefundEmail(ReturnRequest r, decimal amount, bool isCod, string? refundId) =>
        $"""
        <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1f2937">
          <h2 style="font-size:22px;font-weight:700;color:#16a34a;margin-bottom:8px">ðŸ’¸ Your refund has been processed!</h2>
          <p style="color:#6b7280;font-size:14px;margin-bottom:20px">Order #{r.OrderId} . Return Request #{r.Id}</p>
          <p style="font-size:15px;line-height:1.6;margin-bottom:16px">
            We have processed your refund of <strong>Rs.{amount:N0}</strong> for the return of your order.
            {(isCod ? "Since your original payment was Cash on Delivery, the refund will be transferred to your bank account. Our team will contact you for bank details if not already provided." : "The amount will be credited to your original payment method within 5-7 business days.")}
          </p>
          <div style="background:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;padding:16px;margin-bottom:20px">
            <p style="margin:0 0 6px;font-size:13px"><strong>Refund amount:</strong> Rs.{amount:N0}</p>
            <p style="margin:0 0 6px;font-size:13px"><strong>Reason:</strong> {r.Reason}</p>
            {(!string.IsNullOrWhiteSpace(refundId) ? $"<p style='margin:0;font-size:13px'><strong>Refund ID:</strong> {refundId}</p>" : "")}
          </div>
          <p style="font-size:13px;color:#6b7280">If you have any questions, please reply to this email or contact our support team.</p>
          <hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0"/>
          <p style="font-size:12px;color:#9ca3af;text-align:center">Niroté · Handcrafted Jewellery</p>
        </div>
        """;

    private static string BuildReturnEmail(ReturnRequest r, string status, string? adminNote)
    {
        var isApproved = status == "Approved";
        var color = isApproved ? "#16a34a" : "#dc2626";
        var icon = isApproved ? "[Approved]" : "[Rejected]";
        var heading = isApproved ? "Your return has been approved!" : "Your return request was not approved";
        var body = isApproved
            ? "Great news! We have reviewed your return request and it has been approved. Our team will get in touch with you shortly with the next steps for returning your item."
            : "Thank you for your patience. After reviewing your return request, we are unable to approve it at this time.";

        return $"""
            <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1f2937">
              <h2 style="font-size:22px;font-weight:700;color:{color};margin-bottom:8px">{icon} {heading}</h2>
              <p style="color:#6b7280;font-size:14px;margin-bottom:20px">Order #{r.OrderId} . Return Request #{r.Id}</p>
              <p style="font-size:15px;line-height:1.6;margin-bottom:16px">{body}</p>
              <div style="background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;padding:16px;margin-bottom:20px">
                <p style="margin:0 0 6px;font-size:13px"><strong>Reason:</strong> {r.Reason}</p>
                {(r.Description != null ? $"<p style='margin:0 0 6px;font-size:13px'><strong>Your description:</strong> {r.Description}</p>" : "")}
                {(!string.IsNullOrWhiteSpace(adminNote) ? $"<p style='margin:0;font-size:13px'><strong>Note from us:</strong> {adminNote}</p>" : "")}
              </div>
              <p style="font-size:13px;color:#6b7280">If you have any questions, please reply to this email or contact our support team.</p>
              <hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0"/>
              <p style="font-size:12px;color:#9ca3af;text-align:center">Niroté · Handcrafted Jewellery</p>
            </div>
            """;
    }

    private static ReturnRequestDto Map(ReturnRequest r) => new()
    {
        Id = r.Id,
        OrderId = r.OrderId,
        UserId = r.UserId,
        UserName = r.UserName,
        Reason = r.Reason,
        Description = r.Description,
        Status = r.Status,
        AdminNote = r.AdminNote,
        RefundStatus = r.RefundStatus,
        RazorpayRefundId = r.RazorpayRefundId,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
    };
}
