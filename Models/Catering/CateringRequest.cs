namespace Camonda_hands_on.Models.Catering;

public class CateringRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime AgreementStartDate { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Approval properties
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Rejection properties
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }

    // Withdrawal properties
    public string? WithdrawReason { get; set; }
    public DateTime? WithdrawnAt { get; set; }

    // Timeout properties
    public DateTime? TimeoutAt { get; set; }

    // Additional data
    public Dictionary<string, object>? AdditionalData { get; set; }
}