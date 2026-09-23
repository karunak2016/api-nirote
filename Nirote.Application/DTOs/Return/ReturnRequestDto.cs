namespace Nirote.Application.DTOs.Return;

public class ReturnRequestDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending";
    public string? AdminNote { get; set; }
    public string RefundStatus { get; set; } = "None";
    public string? RazorpayRefundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateReturnRequestDto
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateReturnStatusDto
{
    public string Status { get; set; } = string.Empty; // Approved | Rejected
    public string? AdminNote { get; set; }
}
