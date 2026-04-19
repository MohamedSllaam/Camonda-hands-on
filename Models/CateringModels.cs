namespace Camonda_hands_on.Models;

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
