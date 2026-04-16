namespace Camonda_hands_on.Models;
 
public class StartCateringRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public DateTime AgreementStartDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Department { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class ApprovalRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class RejectionRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? RejectedBy { get; set; }
}

public class WithdrawRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? WithdrawnBy { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

public class ProcessStartResponse
{
    public long ProcessInstanceKey { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = "STARTED";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}


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