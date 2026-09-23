namespace Nirote.Domain.Entities;

public class ReturnRequest
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
    public string? AdminNote { get; set; }
    public string RefundStatus { get; set; } = "None"; // None | Processing | Issued | Failed
    public string? RazorpayRefundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
