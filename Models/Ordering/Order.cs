namespace Camonda_hands_on.Models.Ordering;

public class Order
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public decimal Price { get; set; }
    public string OrderRequestId { get; set; } = Guid.NewGuid().ToString();
    public string OrderStatus { get; set; } = "Pending";

    // Additional properties for tracking
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public string? WithdrawReason { get; set; }
    public DateTime? TimeoutAt { get; set; }
   
}
