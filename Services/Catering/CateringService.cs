using Camonda_hands_on.Services.Interfaces;
using System.Collections.Concurrent;
using Camonda_hands_on.Models.Catering;

namespace Camonda_hands_on.Services.Catering;


public class CateringService : ICateringService
{
    private readonly ILogger<CateringService> _logger;
    private static readonly ConcurrentDictionary<string, CateringRequest> _requests = new();
    private static readonly ConcurrentDictionary<string, List<string>> _requestHistory = new();

    public CateringService(ILogger<CateringService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateRequestAsync(string requestId, decimal amount)
    {
        _logger.LogInformation("Validating catering request {RequestId} for amount {Amount:C}", requestId, amount);

        var isValid = amount <= 10000;

        AddHistory(requestId, $"Validation completed: {(isValid ? "PASSED" : "FAILED")} for amount {amount:C}");

        _logger.LogInformation("Request {RequestId} validation result: {IsValid}", requestId, isValid);
        return await Task.FromResult(isValid);
    }

    public async Task ProcessApprovalAsync(string requestId, string approvedBy)
    {
        _logger.LogInformation("Processing approval for catering request {RequestId} by {ApprovedBy}", requestId, approvedBy);

        // ✅ Now we can modify because CateringRequest has setters
        if (_requests.TryGetValue(requestId, out var request))
        {
            request.Status = "APPROVED";
            request.ApprovedBy = approvedBy;
            request.ApprovedAt = DateTime.UtcNow;
            _logger.LogInformation("Request {RequestId} status updated to APPROVED", requestId);
        }
        else
        {
            _logger.LogWarning("Request {RequestId} not found in storage", requestId);
        }

        AddHistory(requestId, $"Request APPROVED by {approvedBy}");

        await NotifyCateringProviderAsync(requestId, "APPROVAL", new { ApprovedBy = approvedBy, Timestamp = DateTime.UtcNow });

        _logger.LogInformation("Request {RequestId} approved successfully", requestId);
    }

    public async Task ProcessRejectionAsync(string requestId, string reason)
    {
        _logger.LogWarning("Rejecting catering request {RequestId} because: {Reason}", requestId, reason);

        if (_requests.TryGetValue(requestId, out var request))
        {
            request.Status = "REJECTED";
            request.RejectionReason = reason;
            request.RejectedAt = DateTime.UtcNow;
        }

        AddHistory(requestId, $"Request REJECTED: {reason}");

        await NotifyCateringProviderAsync(requestId, "REJECTION", new { Reason = reason, Timestamp = DateTime.UtcNow });
    }

    public async Task ProcessWithdrawAsync(string requestId, string reason)
    {
        _logger.LogInformation("Processing withdrawal for catering request {RequestId} because: {Reason}", requestId, reason);

        if (_requests.TryGetValue(requestId, out var request))
        {
            request.Status = "WITHDRAWN";
            request.WithdrawReason = reason;
            request.WithdrawnAt = DateTime.UtcNow;
        }

        AddHistory(requestId, $"Request WITHDRAWN: {reason}");

        await NotifyCateringProviderAsync(requestId, "WITHDRAWAL", new { Reason = reason, Timestamp = DateTime.UtcNow });
    }

    public async Task ProcessTimeoutAsync(string requestId)
    {
        _logger.LogError("Catering request {RequestId} timed out - no response received", requestId);

        if (_requests.TryGetValue(requestId, out var request))
        {
            request.Status = "TIMEOUT";
            request.TimeoutAt = DateTime.UtcNow;
        }

        AddHistory(requestId, "Request TIMED OUT - no response from provider");

        await NotifyCateringProviderAsync(requestId, "TIMEOUT", new { Message = "No response within timeframe", Timestamp = DateTime.UtcNow });
    }

    public async Task NotifyCateringProviderAsync(string requestId, string messageType, object data)
    {
        _logger.LogInformation("Notifying catering provider about {MessageType} for request {RequestId}", messageType, requestId);
        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetRequestStatusAsync(string requestId)
    {
        var result = new Dictionary<string, object>();

        if (_requests.TryGetValue(requestId, out var request))
        {
            result["RequestId"] = request.RequestId;
            result["Status"] = request.Status;
            result["CustomerName"] = request.CustomerName;
            result["CustomerEmail"] = request.CustomerEmail;
            result["TotalAmount"] = request.TotalAmount;
            result["CreatedAt"] = request.CreatedAt;
            result["AgreementStartDate"] = request.AgreementStartDate;

            if (request.ApprovedBy != null)
                result["ApprovedBy"] = request.ApprovedBy;
            if (request.ApprovedAt != null)
                result["ApprovedAt"] = request.ApprovedAt;
            if (request.RejectionReason != null)
                result["RejectionReason"] = request.RejectionReason;
            if (request.WithdrawReason != null)
                result["WithdrawReason"] = request.WithdrawReason;

            if (_requestHistory.TryGetValue(requestId, out var history))
            {
                result["History"] = history;
            }
        }

        return await Task.FromResult(result);
    }

    private void AddHistory(string requestId, string entry)
    {
        var historyEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {entry}";

        _requestHistory.AddOrUpdate(requestId,
            new List<string> { historyEntry },
            (key, existing) => { existing.Add(historyEntry); return existing; });
    }

    public void StoreRequest(string requestId, StartCateringRequest request)
    {
        // ✅ Using concrete class instead of anonymous object
        var cateringRequest = new CateringRequest
        {
            RequestId = request.RequestId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            TotalAmount = request.TotalAmount,
            AgreementStartDate = request.AgreementStartDate,
            Department = request.Department,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,
            AdditionalData = request.AdditionalData ?? new Dictionary<string, object>()
        };

        _requests[requestId] = cateringRequest;
        AddHistory(requestId, "Request created and process started");

        _logger.LogInformation("Stored catering request {RequestId} for customer {CustomerName}",
            requestId, request.CustomerName);
    }
}