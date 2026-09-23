using Nirote.Application.DTOs.Order;
using Nirote.Application.DTOs.Product;
using Nirote.Application.Interfaces;
using Nirote.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Nirote.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;
    private readonly ICartRepository _cartRepo;
    private readonly ICouponService _coupons;
    private readonly IEmailService _email;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repo, ICartRepository cartRepo, ICouponService coupons, IEmailService email, ILogger<OrderService> logger)
    {
        _repo = repo;
        _cartRepo = cartRepo;
        _coupons = coupons;
        _email = email;
        _logger = logger;
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(int userId)
    {
        try
        {
            _logger.LogInformation("Get orders for user {UserId}", userId);
            var (items, _) = await _repo.GetByUserAsync(userId, 1, 100);
            return items.Select(o => MapToDto(o)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get orders failed for user {UserId}", userId);
            throw;
        }
    }

    public async Task<OrderDto?> GetByIdAsync(int orderId, int userId)
    {
        try
        {
            _logger.LogInformation("Get order {OrderId} for user {UserId}", orderId, userId);
            var (header, items) = await _repo.GetByIdAsync(orderId);
            if (header == null || header.UserId != userId) return null;
            return MapToDto(header, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get order {OrderId} failed for user {UserId}", orderId, userId);
            throw;
        }
    }

    public async Task<OrderDto?> GetByIdAdminAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Admin get order {OrderId}", orderId);
            var (header, items) = await _repo.GetByIdAsync(orderId);
            if (header == null) return null;
            return MapToDto(header, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin get order {OrderId} failed", orderId);
            throw;
        }
    }

    public async Task<OrderDto> PlaceOrderAsync(int userId, PlaceOrderDto dto)
    {
        try
        {
            _logger.LogInformation("Place order for user {UserId}", userId);
            var (_, cartItems) = await _cartRepo.GetWithItemsAsync(userId);
            var itemList = cartItems.ToList();
            if (!itemList.Any()) throw new InvalidOperationException("Cart is empty.");

            var total = itemList.Sum(i => i.UnitPrice * i.Quantity);

            // Apply coupon discount
            decimal discountAmount = 0;
            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var validation = await _coupons.ValidateAsync(dto.CouponCode, total);
                if (validation.IsValid)
                {
                    discountAmount = validation.DiscountAmount;
                    await _coupons.IncrementUsageAsync(validation.CouponId!.Value);
                    _logger.LogInformation("Coupon {Code} applied: Rs.{Discount} off order for user {UserId}",
                        dto.CouponCode, discountAmount, userId);
                }
            }

            var shippingFee = dto.ShippingFee;
            var finalAmount = total - discountAmount + shippingFee;

            var itemsJson = itemList.Select(i => (object)new
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName ?? string.Empty,
                ProductImage = i.DefaultImage ?? string.Empty,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToArray();

            var orderId = await _repo.PlaceAsync(userId, dto.AddressId, dto.PaymentMethod,
                total, discountAmount, shippingFee, finalAmount, null, itemsJson);

            var (header, orderItems) = await _repo.GetByIdAsync(orderId);
            _logger.LogInformation("Order {OrderId} placed for user {UserId}", orderId, userId);

            var orderDto = MapToDto(header!, orderItems);

            // For COD: clear cart and send confirmation immediately
            // For Razorpay: cart is cleared and email sent only after payment is verified
            if (dto.PaymentMethod == "COD")
            {
                await _cartRepo.ClearAsync(userId);
                if (!string.IsNullOrWhiteSpace(header!.CustomerEmail))
                {
                    _ = Task.Run(async () =>
                    {
                        try { await SendOrderConfirmationAsync(orderDto, header.CustomerName ?? "Valued Customer"); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Order confirmation email failed for order {OrderId}", orderId); }
                    });
                }
            }

            return orderDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Place order failed for user {UserId}", userId);
            throw;
        }
    }

    public async Task SendConfirmationEmailAsync(int orderId)
    {
        var (header, items) = await _repo.GetByIdAsync(orderId);
        if (header == null || string.IsNullOrWhiteSpace(header.CustomerEmail)) return;
        var orderDto = MapToDto(header, items);
        _ = Task.Run(async () =>
        {
            try { await SendOrderConfirmationAsync(orderDto, header.CustomerName ?? "Valued Customer"); }
            catch (Exception ex) { _logger.LogWarning(ex, "Order confirmation email failed for order {OrderId}", orderId); }
        });
    }

    public async Task CancelOrderAsync(int orderId, int userId, string? reason = null)
    {
        try
        {
            _logger.LogInformation("Cancel order {OrderId} for user {UserId}", orderId, userId);
            var (success, error) = await _repo.CancelAsync(orderId, userId, reason);
            if (!success) throw new InvalidOperationException(error ?? "Cannot cancel order.");
            _logger.LogInformation("Order {OrderId} cancelled for user {UserId}", orderId, userId);

            var (header, items) = await _repo.GetByIdAsync(orderId);
            if (header != null && !string.IsNullOrWhiteSpace(header.CustomerEmail))
            {
                var orderDto = MapToDto(header, items);
                var name = header.CustomerName ?? "Valued Customer";
                _ = Task.Run(async () =>
                {
                    try { await SendCancellationEmailAsync(orderDto, name, reason); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Cancellation email failed for order {OrderId}", orderId); }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cancel order {OrderId} failed for user {UserId}", orderId, userId);
            throw;
        }
    }

    public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page, int pageSize)
    {
        try
        {
            _logger.LogInformation("Admin get all orders page={Page}", page);
            var (items, total) = await _repo.GetAllAsync(null, null, null, null, page, pageSize);
            return new PagedResult<OrderDto>
            {
                Items = items.Select(o => MapToDto(o)).ToList(),
                TotalCount = total, Page = page, PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get all orders failed");
            throw;
        }
    }

    public async Task UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto dto)
    {
        try
        {
            _logger.LogInformation("Update status of order {OrderId} to {Status}", orderId, dto.Status);
            await _repo.UpdateStatusAsync(orderId, dto.Status, string.IsNullOrWhiteSpace(dto.PaymentStatus) ? null : dto.PaymentStatus, null, string.IsNullOrWhiteSpace(dto.AwbCode) ? null : dto.AwbCode);
            _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, dto.Status);

            if (dto.Status == "Shipped" || dto.Status == "Delivered")
            {
                var (header, items) = await _repo.GetByIdAsync(orderId);
                if (header != null && !string.IsNullOrWhiteSpace(header.CustomerEmail))
                {
                    var orderDto = MapToDto(header, items);
                    var awb = dto.AwbCode;
                    var name = header.CustomerName ?? "Valued Customer";
                    if (dto.Status == "Shipped")
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await SendShippingNotificationAsync(orderDto, name, awb); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Shipping email failed for order {OrderId}", orderId); }
                        });
                    }
                    else
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await SendDeliveryNotificationAsync(orderDto, name); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Delivery email failed for order {OrderId}", orderId); }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update order {OrderId} status failed", orderId);
            throw;
        }
    }

    private async Task SendCancellationEmailAsync(OrderDto order, string customerName, string? reason)
    {
        var reasonSection = !string.IsNullOrWhiteSpace(reason)
            ? $@"<div style=""margin:16px 0;padding:14px 16px;background:#fdf2f2;border-radius:8px;border-left:3px solid #e57373"">
                   <p style=""margin:0;font-size:13px;color:#555""><strong>Reason:</strong> {reason}</p>
                 </div>"
            : "";

        var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family:sans-serif;background:#faf9f7;margin:0;padding:0"">
  <div style=""max-width:520px;margin:40px auto;background:#fff;border:1px solid #e8e3db;border-radius:12px;overflow:hidden"">
    <div style=""background:#2d3a2e;padding:28px 32px;text-align:center"">
      <span style=""font-family:Georgia,serif;font-size:28px;font-weight:bold;color:#c9a84c;letter-spacing:0.06em"">Niroté</span>
    </div>
    <div style=""padding:32px"">
      <h2 style=""font-family:Georgia,serif;color:#1a1008;margin:0 0 8px"">Order Cancelled</h2>
      <p style=""color:#555;font-size:14px;margin:0 0 16px"">Hi {customerName}, your Order <strong>#{order.Id}</strong> has been cancelled.</p>
      {reasonSection}
      <p style=""color:#555;font-size:13px;line-height:1.7;margin:16px 0"">
        If you paid online, your refund will be processed within 5–7 business days to your original payment method.
      </p>
      <p style=""color:#555;font-size:13px;line-height:1.7;margin:0"">
        If you have any questions, please contact us at <a href=""mailto:hello@nirote.com"" style=""color:#2d3a2e"">hello@nirote.com</a>.
      </p>
    </div>
  </div>
</body>
</html>";

        await _email.SendAsync(order.CustomerEmail!, $"Order #{order.Id} Cancelled | Niroté", html);
    }

    private async Task SendDeliveryNotificationAsync(OrderDto order, string customerName)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family:sans-serif;background:#faf9f7;margin:0;padding:0"">
  <div style=""max-width:520px;margin:40px auto;background:#fff;border:1px solid #e8e3db;border-radius:12px;overflow:hidden"">
    <div style=""background:#2d3a2e;padding:28px 32px;text-align:center"">
      <span style=""font-family:Georgia,serif;font-size:28px;font-weight:bold;color:#c9a84c;letter-spacing:0.06em"">Niroté</span>
    </div>
    <div style=""padding:32px"">
      <h2 style=""font-family:Georgia,serif;color:#1a1008;margin:0 0 8px"">Your order has been delivered!</h2>
      <p style=""color:#555;font-size:14px;margin:0 0 20px"">Hi {customerName}, your Order #{order.Id} has been delivered successfully.</p>
      <p style=""color:#555;font-size:13px;line-height:1.7;margin:16px 0"">
        We hope you love your Niroté jewellery. If you have any questions or concerns about your order, please don't hesitate to reach out to us.
      </p>
      <div style=""text-align:center;margin:24px 0"">
        <a href=""https://nirote.com/orders/{order.Id}"" style=""display:inline-block;background:#2d3a2e;color:#fff;text-decoration:none;padding:12px 28px;border-radius:999px;font-size:13px;font-weight:600"">
          View Order
        </a>
      </div>
      <p style=""color:#aaa;font-size:12px;margin:0"">Questions? Email us at <a href=""mailto:hello@nirote.com"" style=""color:#aaa"">hello@nirote.com</a></p>
    </div>
  </div>
</body>
</html>";

        await _email.SendAsync(order.CustomerEmail!, $"Your Order #{order.Id} Has Been Delivered! | Niroté", html);
    }

    private async Task SendShippingNotificationAsync(OrderDto order, string customerName, string? awbCode)
    {
        var trackingSection = !string.IsNullOrWhiteSpace(awbCode)
            ? $@"<div style=""margin:20px 0;padding:16px;background:#f5f0e8;border-radius:8px;text-align:center"">
                   <p style=""margin:0 0 4px;font-size:12px;color:#888;text-transform:uppercase;letter-spacing:0.1em"">Tracking Number</p>
                   <p style=""margin:0;font-size:18px;font-weight:bold;color:#2d3a2e;letter-spacing:0.05em"">{awbCode}</p>
                 </div>"
            : "";

        var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family:sans-serif;background:#faf9f7;margin:0;padding:0"">
  <div style=""max-width:520px;margin:40px auto;background:#fff;border:1px solid #e8e3db;border-radius:12px;overflow:hidden"">
    <div style=""background:#2d3a2e;padding:28px 32px;text-align:center"">
      <span style=""font-family:Georgia,serif;font-size:28px;font-weight:bold;color:#c9a84c;letter-spacing:0.06em"">Niroté</span>
    </div>
    <div style=""padding:32px"">
      <h2 style=""font-family:Georgia,serif;color:#1a1008;margin:0 0 8px"">Your order is on its way!</h2>
      <p style=""color:#555;font-size:14px;margin:0 0 20px"">Hi {customerName}, your Order #{order.Id} has been shipped.</p>
      {trackingSection}
      <p style=""color:#555;font-size:13px;line-height:1.7;margin:16px 0"">
        Your jewellery is on its way to you. Deliveries typically take 3–7 business days depending on your location.
      </p>
      <div style=""text-align:center;margin:24px 0"">
        <a href=""https://nirote.com/orders/{order.Id}"" style=""display:inline-block;background:#2d3a2e;color:#fff;text-decoration:none;padding:12px 28px;border-radius:999px;font-size:13px;font-weight:600"">
          Track My Order
        </a>
      </div>
      <p style=""color:#aaa;font-size:12px;margin:0"">Questions? Email us at <a href=""mailto:hello@nirote.com"" style=""color:#aaa"">hello@nirote.com</a></p>
    </div>
  </div>
</body>
</html>";

        await _email.SendAsync(order.CustomerEmail!, $"Your Order #{order.Id} Has Been Shipped! | Niroté", html);
    }

    private async Task SendOrderConfirmationAsync(OrderDto order, string customerName)
    {
        var itemRows = string.Join("", order.Items.Select(i => $@"
        <tr>
          <td style=""padding:10px 0;border-bottom:1px solid #f0ede8;font-size:13px;color:#333"">{i.ProductName}</td>
          <td style=""padding:10px 0;border-bottom:1px solid #f0ede8;font-size:13px;color:#333;text-align:center"">{i.Quantity}</td>
          <td style=""padding:10px 0;border-bottom:1px solid #f0ede8;font-size:13px;color:#333;text-align:right"">₹{i.Subtotal:N0}</td>
        </tr>"));

        var discountRow = order.DiscountAmount > 0
            ? $@"<tr><td colspan=""2"" style=""font-size:13px;color:#555;padding:6px 0"">Discount</td><td style=""font-size:13px;color:#2a8a2a;text-align:right;padding:6px 0"">–₹{order.DiscountAmount:N0}</td></tr>"
            : "";

        var shippingRow = order.ShippingFee > 0
            ? $@"<tr><td colspan=""2"" style=""font-size:13px;color:#555;padding:6px 0"">Shipping</td><td style=""font-size:13px;color:#333;text-align:right;padding:6px 0"">₹{order.ShippingFee:N0}</td></tr>"
            : @"<tr><td colspan=""2"" style=""font-size:13px;color:#555;padding:6px 0"">Shipping</td><td style=""font-size:13px;color:#2a8a2a;text-align:right;padding:6px 0"">FREE</td></tr>";

        var html = $@"
<!DOCTYPE html>
<html>
<body style=""font-family:sans-serif;background:#faf9f7;margin:0;padding:0"">
  <div style=""max-width:520px;margin:40px auto;background:#fff;border:1px solid #e8e3db;border-radius:12px;overflow:hidden"">
    <div style=""background:#2d3a2e;padding:28px 32px;text-align:center"">
      <span style=""font-family:Georgia,serif;font-size:28px;font-weight:bold;color:#c9a84c;letter-spacing:0.06em"">Niroté</span>
    </div>
    <div style=""padding:32px"">
      <h2 style=""font-family:Georgia,serif;color:#1a1008;margin:0 0 6px"">Order Confirmed!</h2>
      <p style=""color:#555;font-size:14px;margin:0 0 20px"">Hi {customerName}, your order <strong>#{order.Id}</strong> has been placed successfully.</p>

      <table style=""width:100%;border-collapse:collapse"">
        <thead>
          <tr style=""border-bottom:2px solid #e8e3db"">
            <th style=""text-align:left;font-size:11px;text-transform:uppercase;letter-spacing:0.1em;color:#888;padding-bottom:8px"">Item</th>
            <th style=""text-align:center;font-size:11px;text-transform:uppercase;letter-spacing:0.1em;color:#888;padding-bottom:8px"">Qty</th>
            <th style=""text-align:right;font-size:11px;text-transform:uppercase;letter-spacing:0.1em;color:#888;padding-bottom:8px"">Amount</th>
          </tr>
        </thead>
        <tbody>{itemRows}</tbody>
      </table>

      <table style=""width:100%;border-collapse:collapse;margin-top:12px"">
        {discountRow}
        {shippingRow}
        <tr style=""border-top:2px solid #e8e3db"">
          <td colspan=""2"" style=""font-size:14px;font-weight:700;color:#1a1008;padding-top:10px"">Total</td>
          <td style=""font-size:16px;font-weight:700;color:#2d3a2e;text-align:right;padding-top:10px"">₹{order.FinalAmount:N0}</td>
        </tr>
      </table>

      <div style=""margin:24px 0;padding:16px;background:#faf9f7;border-radius:8px;border:1px solid #e8e3db"">
        <p style=""font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:0.1em;color:#888;margin:0 0 6px"">Payment Method</p>
        <p style=""font-size:14px;color:#333;margin:0"">{order.PaymentMethod}</p>
      </div>

      <div style=""text-align:center;margin:24px 0"">
        <a href=""https://nirote.com/orders/{order.Id}"" style=""display:inline-block;background:#2d3a2e;color:#fff;text-decoration:none;padding:12px 28px;border-radius:999px;font-size:13px;font-weight:600"">
          Track My Order
        </a>
      </div>

      <p style=""color:#aaa;font-size:12px;line-height:1.6;margin:0"">
        We'll send you an update once your order is dispatched.<br/>
        Questions? Email us at <a href=""mailto:hello@nirote.com"" style=""color:#888"">hello@nirote.com</a>
      </p>
    </div>
  </div>
</body>
</html>";

        await _email.SendAsync(order.CustomerEmail!, $"Order Confirmed #{order.Id} | Niroté", html);
    }

    private static OrderDto MapToDto(Order o, IEnumerable<OrderItem>? items = null) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        ItemCount = o.ItemCount,
        TotalAmount = o.TotalAmount,
        DiscountAmount = o.DiscountAmount,
        ShippingFee = o.ShippingFee,
        FinalAmount = o.FinalAmount,
        PaymentMethod = o.PaymentMethod,
        PaymentStatus = o.PaymentStatus,
        OrderStatus = o.OrderStatus,
        AWBCode = o.AWBCode,
        CreatedAt = o.CreatedAt,
        DeliveryName = o.DeliveryName,
        DeliveryPhone = o.DeliveryPhone,
        AddressLine1 = o.AddressLine1,
        AddressLine2 = o.AddressLine2,
        City = o.City,
        State = o.State,
        Pincode = o.Pincode,
        Items = (items ?? Enumerable.Empty<OrderItem>()).Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ImageUrl = i.ProductImage,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Subtotal = i.Subtotal
        }).ToList()
    };
}

public interface IOrderRepository
{
    Task<(IEnumerable<Order> Items, int Total)> GetByUserAsync(int userId, int page, int pageSize);
    Task<(Order? Header, IEnumerable<OrderItem> Items)> GetByIdAsync(int orderId);
    Task<int> PlaceAsync(int userId, int addressId, string paymentMethod,
        decimal totalAmount, decimal discountAmount, decimal shippingFee, decimal finalAmount,
        string? notes, object[] items);
    Task<(bool Success, string? Error)> CancelAsync(int orderId, int userId, string? cancelReason);
    Task<(IEnumerable<Order> Items, int Total)> GetAllAsync(string? orderStatus, string? paymentStatus,
        DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task UpdateStatusAsync(int orderId, string? orderStatus, string? paymentStatus,
        string? shiprocketOrderId, string? awbCode);
    Task SaveRazorpayPaymentIdAsync(int orderId, string paymentId);
    Task<bool> HasPurchasedAsync(int userId, int productId);
}
