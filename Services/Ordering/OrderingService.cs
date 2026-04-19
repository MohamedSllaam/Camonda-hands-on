using Camonda_hands_on.Services.Interfaces;

namespace Camonda_hands_on.Services.Ordering;
using global::Camonda_hands_on.Models.Ordering;
using System.Collections.Concurrent;


public class OrderingService : IOrderingService
{
    private readonly ILogger<OrderingService> _logger;
    private static readonly ConcurrentDictionary<string, Order> _orders = new();
    private static readonly ConcurrentDictionary<string, List<string>> _orderHistory = new();

    public OrderingService(ILogger<OrderingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateOrderAsync(string orderRequestId, decimal amount)
    {
        _logger.LogInformation("Validating order {OrderRequestId} for amount {Amount:C}", orderRequestId, amount);

        // Business rules: Maximum order amount validation
        var isValid = amount <= 50000; // $50,000 max order value

        AddHistory(orderRequestId, $"Validation completed: {(isValid ? "PASSED" : "FAILED")} for amount {amount:C}");

        _logger.LogInformation("Order {OrderRequestId} validation result: {IsValid}", orderRequestId, isValid);
        return await Task.FromResult(isValid);
    }

    public async Task ProcessApprovalAsync(string orderRequestId, string approvedBy)
    {
        _logger.LogInformation("Processing approval for order {OrderRequestId} by {ApprovedBy}", orderRequestId, approvedBy);

        if (_orders.TryGetValue(orderRequestId, out var order))
        {
            order.OrderStatus = "APPROVED";
            order.ApprovedBy = approvedBy;
            order.ApprovedAt = DateTime.UtcNow;
            _orders[orderRequestId] = order;

            _logger.LogInformation("Order {OrderRequestId} status updated to APPROVED", orderRequestId);
        }
        else
        {
            _logger.LogWarning("Order {OrderRequestId} not found in storage", orderRequestId);
        }

        AddHistory(orderRequestId, $"Order APPROVED by {approvedBy}");

        await NotifyProviderAsync(orderRequestId, "APPROVAL", new { ApprovedBy = approvedBy, Timestamp = DateTime.UtcNow });

        _logger.LogInformation("Order {OrderRequestId} approved successfully", orderRequestId);
    }

    public async Task ProcessRejectionAsync(string orderRequestId, string reason)
    {
        _logger.LogWarning("Rejecting order {OrderRequestId} because: {Reason}", orderRequestId, reason);

        if (_orders.TryGetValue(orderRequestId, out var order))
        {
            order.OrderStatus = "REJECTED";
            order.RejectionReason = reason;
            order.RejectedAt = DateTime.UtcNow;
            _orders[orderRequestId] = order;
        }

        AddHistory(orderRequestId, $"Order REJECTED: {reason}");

        await NotifyProviderAsync(orderRequestId, "REJECTION", new { Reason = reason, Timestamp = DateTime.UtcNow });
    }

    public async Task ProcessWithdrawAsync(string orderRequestId, string reason)
    {
        _logger.LogInformation("Processing withdrawal for order {OrderRequestId} because: {Reason}", orderRequestId, reason);

        if (_orders.TryGetValue(orderRequestId, out var order))
        {
            order.OrderStatus = "WITHDRAWN";
            order.WithdrawReason = reason;
            order.WithdrawnAt = DateTime.UtcNow;
            _orders[orderRequestId] = order;
        }

        AddHistory(orderRequestId, $"Order WITHDRAWN: {reason}");

        await NotifyProviderAsync(orderRequestId, "WITHDRAWAL", new { Reason = reason, Timestamp = DateTime.UtcNow });
    }

    public async Task ProcessTimeoutAsync(string orderRequestId)
    {
        _logger.LogError("Order {OrderRequestId} timed out - no response received", orderRequestId);

        if (_orders.TryGetValue(orderRequestId, out var order))
        {
            order.OrderStatus = "TIMEOUT";
            order.TimeoutAt = DateTime.UtcNow;
            _orders[orderRequestId] = order;
        }

        AddHistory(orderRequestId, "Order TIMED OUT - no response from provider");

        await NotifyProviderAsync(orderRequestId, "TIMEOUT", new { Message = "No response within timeframe", Timestamp = DateTime.UtcNow });
    }

    public async Task NotifyProviderAsync(string orderRequestId, string messageType, object data)
    {
        _logger.LogInformation("Notifying provider about {MessageType} for order {OrderRequestId}", messageType, orderRequestId);

        // In production: Call external API, send to message queue, or webhook
        // Example: await _httpClient.PostAsJsonAsync("https://provider-api.example.com/webhook", new { orderRequestId, messageType, data });

        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> GetRequestStatusAsync(string orderRequestId)
    {
        var result = new Dictionary<string, object>();

        if (_orders.TryGetValue(orderRequestId, out var order))
        {
            result["OrderRequestId"] = order.OrderRequestId;
            result["OrderStatus"] = order.OrderStatus;
            result["OrderItemId"] = order.OrderItemId;
            result["Price"] = order.Price;
            result["CreatedAt"] = order.CreatedAt;

            if (!string.IsNullOrEmpty(order.CustomerName))
                result["CustomerName"] = order.CustomerName;
            if (!string.IsNullOrEmpty(order.CustomerEmail))
                result["CustomerEmail"] = order.CustomerEmail;
            if (order.ApprovedBy != null)
                result["ApprovedBy"] = order.ApprovedBy;
            if (order.ApprovedAt != null)
                result["ApprovedAt"] = order.ApprovedAt;
            if (order.RejectionReason != null)
                result["RejectionReason"] = order.RejectionReason;
            if (order.WithdrawReason != null)
                result["WithdrawReason"] = order.WithdrawReason;

            if (_orderHistory.TryGetValue(orderRequestId, out var history))
            {
                result["History"] = history;
            }
        }
        else
        {
            _logger.LogWarning("Order {OrderRequestId} not found", orderRequestId);
        }

        return await Task.FromResult(result);
    }

    private void AddHistory(string orderRequestId, string entry)
    {
        var historyEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {entry}";

        _orderHistory.AddOrUpdate(orderRequestId,
            new List<string> { historyEntry },
            (key, existing) => { existing.Add(historyEntry); return existing; });
    }

    public void StoreRequest(string orderRequestId, CreateOrderRequest request)
    {
        var order = new Order
        {
            OrderRequestId = request.OrderRequestId,
            OrderItemId = request.OrderItemId,
            Price = request.Price,
            OrderStatus = "PENDING",
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CreatedAt = DateTime.UtcNow
        };

        _orders[orderRequestId] = order;
        AddHistory(orderRequestId, "Order created and process started");

        _logger.LogInformation("Stored order {OrderRequestId} for customer {CustomerName}, Amount: {Price:C}",
            orderRequestId, request.CustomerName ?? "Anonymous", request.Price);
    }
}