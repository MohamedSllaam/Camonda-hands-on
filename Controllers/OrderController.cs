using Microsoft.AspNetCore.Mvc;
using Zeebe.Client;
using Camonda_hands_on.Models.Ordering;
using Camonda_hands_on.Services.Interfaces;
using System.Text.Json;

namespace Camonda_hands_on.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IZeebeClient _zeebeClient;
    private readonly IOrderingService _orderingService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IZeebeClient zeebeClient,
        IOrderingService orderingService,
        ILogger<OrderController> logger)
    {
        _zeebeClient = zeebeClient;
        _orderingService = orderingService;
        _logger = logger;
    }

    /// <summary>
    /// Step 1: Create a new process instance (creates the workflow)
    /// </summary>
    [HttpPost("create-process")]
    public async Task<IActionResult> CreateProcessInstance([FromBody] CreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Creating process instance for OrderRequestId: {OrderRequestId}", request.OrderRequestId);

            // Store the order
            _orderingService.StoreRequest(request.OrderRequestId, request);

            // Prepare initial variables
            var variables = new Dictionary<string, object>
            {
                ["orderRequestId"] = request.OrderRequestId,
                ["orderItemId"] = request.OrderItemId,
                ["price"] = request.Price,
                ["timeInMinutes"] = 5,  // Timeout in minutes from BPMN
                ["orderStatus"] = "Pending",
                ["isEnough"] = false,
                ["customerName"] = request.CustomerName ?? "Anonymous",
                ["customerEmail"] = request.CustomerEmail ?? ""
            };

            // Create process instance
            var processResult = await _zeebeClient.NewCreateProcessInstanceCommand()
                .BpmnProcessId("Process_0vv7x24")  // Your BPMN process ID
                .LatestVersion()
                .Variables(JsonSerializer.Serialize(variables))
                .Send();

            _logger.LogInformation("✅ Process instance created: {ProcessInstanceKey}", processResult.ProcessInstanceKey);

            return Ok(new
            {
                Success = true,
                ProcessInstanceKey = processResult.ProcessInstanceKey,
                OrderRequestId = request.OrderRequestId,
                Message = "Process instance created successfully. Use /trigger endpoint to start balance check."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create process instance");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Step 2: Trigger the balance check by sending a message to the waiting process
    /// </summary>
    [HttpPost("trigger/{orderRequestId}")]
    public async Task<IActionResult> TriggerBalanceCheck(string orderRequestId)
    {
        try
        {
            _logger.LogInformation("Triggering balance check for OrderRequestId: {OrderRequestId}", orderRequestId);

            // Get order details to include in the message
            var orderStatus = await _orderingService.GetRequestStatusAsync(orderRequestId);

            if (orderStatus.Count == 0)
            {
                return NotFound(new { Success = false, Error = $"Order {orderRequestId} not found" });
            }

            // Send message to trigger the intermediate message catch event
            await _zeebeClient.NewPublishMessageCommand()
                .MessageName("msg_check_balance_response")
                .CorrelationKey(orderRequestId)
                .Variables(JsonSerializer.Serialize(new
                {
                    orderRequestId = orderRequestId,
                    triggeredAt = DateTime.UtcNow.ToString("O")
                }))
                .TimeToLive(TimeSpan.FromMinutes(1))
                .Send();

            _logger.LogInformation("✅ Balance check triggered for Order: {OrderRequestId}", orderRequestId);

            return Ok(new
            {
                Success = true,
                OrderRequestId = orderRequestId,
                Message = "Balance check triggered successfully. Check worker logs for results."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger balance check");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Combined endpoint - does both steps in one call (convenience method)
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartFullProcess([FromBody] CreateOrderRequest request)
    {
        try
        {
            // Step 1: Create process
            _orderingService.StoreRequest(request.OrderRequestId, request);

            var variables = new Dictionary<string, object>
            {
                ["orderRequestId"] = request.OrderRequestId,
                ["orderItemId"] = request.OrderItemId,
                ["price"] = request.Price,
                ["timeInMinutes"] = 5,
                ["orderStatus"] = "Pending",
                ["isEnough"] = false,
                ["customerName"] = request.CustomerName ?? "Anonymous",
                ["customerEmail"] = request.CustomerEmail ?? ""
            };

            var processResult = await _zeebeClient.NewCreateProcessInstanceCommand()
                .BpmnProcessId("Process_0vv7x24")
                .LatestVersion()
                .Variables(JsonSerializer.Serialize(variables))
                .Send();

            // Step 2: Trigger message
            await _zeebeClient.NewPublishMessageCommand()
                .MessageName("msg_check_balance_response")
                .CorrelationKey(request.OrderRequestId)
                .Variables(JsonSerializer.Serialize(new
                {
                    orderRequestId = request.OrderRequestId,
                    triggeredAt = DateTime.UtcNow.ToString("O")
                }))
                .TimeToLive(TimeSpan.FromMinutes(1))
                .Send();

            _logger.LogInformation("✅ Full process started for Order: {OrderRequestId}", request.OrderRequestId);

            return Ok(new
            {
                Success = true,
                ProcessInstanceKey = processResult.ProcessInstanceKey,
                OrderRequestId = request.OrderRequestId,
                Message = "Order process started successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start full process");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Check process status
    /// </summary>
    [HttpGet("status/{orderRequestId}")]
    public async Task<IActionResult> GetOrderStatus(string orderRequestId)
    {
        try
        {
            var status = await _orderingService.GetRequestStatusAsync(orderRequestId);

            if (status.Count == 0)
            {
                return NotFound(new { Success = false, Error = $"Order {orderRequestId} not found" });
            }

            return Ok(new { Success = true, Data = status });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order status");
            return StatusCode(500, new { Success = false, Error = ex.Message });
        }
    }
}