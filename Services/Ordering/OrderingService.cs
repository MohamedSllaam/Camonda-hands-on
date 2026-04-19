using Camonda_hands_on.Services.Interfaces;

namespace Camonda_hands_on.Services.Ordering;
using global::Camonda_hands_on.Models.Ordering;
using System.Collections.Concurrent;


public class OrderingService : IOrderingService
{
    private readonly ILogger<OrderingService> _logger;
    private static readonly ConcurrentDictionary<string, Order> _orders = new();

    public OrderingService(ILogger<OrderingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateOrderAsync(string orderRequestId, decimal amount)
    {
        _logger.LogInformation("Validating order {OrderRequestId} for amount {Amount:C}", orderRequestId, amount);

        // Business rules: Maximum order amount validation
        var isValid = amount <= 50000; // $50,000 max order value



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

           
        }
        else
        {
            _logger.LogWarning("Order {OrderRequestId} not found", orderRequestId);
        }

        return await Task.FromResult(result);
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


        _logger.LogInformation("Stored order {OrderRequestId} for customer {CustomerName}, Amount: {Price:C}",
            orderRequestId, request.CustomerName ?? "Anonymous", request.Price);
    }

    public async Task<bool> CheckBalanceAsync(string orderRequestId)
    {
        _logger.LogInformation("Checking balance for order {OrderRequestId}", orderRequestId);

        // Generate random boolean for balance check
        var random = new Random();
        var isBalanceEnough = random.Next(2) == 1; // Returns true or false randomly

        // Get the order from storage
        if (_orders.TryGetValue(orderRequestId, out var order))
        {
            if (!isBalanceEnough)
            {
                // Update order status for insufficient balance
                order.OrderStatus = "REJECTED";
                order.RejectionReason = "Insufficient balance";
                order.RejectedAt = DateTime.UtcNow;
                _orders[orderRequestId] = order;

                _logger.LogWarning("Order {OrderRequestId} rejected due to insufficient balance", orderRequestId);
            }
            else
            {
                _logger.LogInformation("Order {OrderRequestId} balance check passed", orderRequestId);
            }
        }
        else
        {
            _logger.LogWarning("Order {OrderRequestId} not found for balance check", orderRequestId);
        }

        return await Task.FromResult(isBalanceEnough);
    }
}