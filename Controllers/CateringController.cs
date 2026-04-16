namespace Camonda_hands_on.Controllers;
using Camonda_hands_on.Models;
using Camonda_hands_on.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Zeebe.Client;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CateringController : ControllerBase
{
    private readonly IZeebeClient _zeebeClient;
    private readonly ICateringService _cateringService;
    private readonly ILogger<CateringController> _logger;

    public CateringController(
        IZeebeClient zeebeClient,
        ICateringService cateringService,
        ILogger<CateringController> logger)
    {
        _zeebeClient = zeebeClient;
        _cateringService = cateringService;
        _logger = logger;
    }

    /// <summary>
    /// Start a new catering agreement process
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<ProcessStartResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> StartProcess([FromBody] StartCateringRequest request)
    {
        try
        {
            _logger.LogInformation("Starting catering agreement process for RequestId: {RequestId}", request.RequestId);

            // Validate request
            if (string.IsNullOrEmpty(request.CustomerName))
                return BadRequest(new ApiResponse<object> { Success = false, Error = "CustomerName is required" });

            if (request.TotalAmount <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Error = "TotalAmount must be greater than 0" });

            // Store the request data
            _cateringService.StoreRequest(request.RequestId, request);

            // Validate business rules
            var isValid = await _cateringService.ValidateRequestAsync(request.RequestId, request.TotalAmount);

            // Prepare variables for the process
            var variables = new Dictionary<string, object>
            {
                ["createRequestId"] = request.RequestId,
                ["agreementStartDate"] = request.AgreementStartDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["customerName"] = request.CustomerName,
                ["customerEmail"] = request.CustomerEmail,
                ["totalAmount"] = request.TotalAmount,
                ["isApproved"] = false,
                ["isValidated"] = isValid,
                ["department"] = request.Department ?? "General",
                ["additionalData"] = request.AdditionalData ?? new Dictionary<string, object>()
            };

            // Start the process instance
            var result = await _zeebeClient.NewCreateProcessInstanceCommand()
                .BpmnProcessId("cat_agreement_create_request")
                .LatestVersion()
                .Variables(JsonSerializer.Serialize(variables))
                .Send();

            _logger.LogInformation("✅ Process started with InstanceKey: {InstanceKey} for RequestId: {RequestId}",
                result.ProcessInstanceKey, request.RequestId);

            var response = new ProcessStartResponse
            {
                ProcessInstanceKey = result.ProcessInstanceKey,
                RequestId = request.RequestId,
                Status = isValid ? "STARTED" : "STARTED_NEEDS_REVIEW"
            };

            return Ok(new ApiResponse<ProcessStartResponse>
            {
                Success = true,
                Message = isValid ? "Catering agreement process initiated successfully" : "Process started but requires review due to amount",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start catering process for RequestId: {RequestId}", request.RequestId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Send approval message to the process
    /// </summary>
    [HttpPost("approve")]
    public async Task<IActionResult> SendApproval([FromBody] ApprovalRequest request)
    {
        try
        {
            _logger.LogInformation("Sending approval for RequestId: {RequestId} by {ApprovedBy}",
                request.RequestId, request.ApprovedBy);

            await _zeebeClient.NewPublishMessageCommand()
                .MessageName("msg_catering_create_response")
                .CorrelationKey(request.RequestId)
                .Variables(JsonSerializer.Serialize(new
                {
                    isApproved = true,
                    approvedBy = request.ApprovedBy,
                    approvalComments = request.Comments,
                    approvalTimestamp = DateTime.UtcNow
                }))
                .TimeToLive(TimeSpan.FromMinutes(1))
                .Send();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"Approval message sent for request {request.RequestId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send approval for RequestId: {RequestId}", request.RequestId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Send rejection message to the process
    /// </summary>
    [HttpPost("reject")]
    public async Task<IActionResult> SendRejection([FromBody] RejectionRequest request)
    {
        try
        {
            _logger.LogWarning("Sending rejection for RequestId: {RequestId} - Reason: {Reason}",
                request.RequestId, request.Reason);

            await _zeebeClient.NewPublishMessageCommand()
                .MessageName("msg_catering_create_response")
                .CorrelationKey(request.RequestId)
                .Variables(JsonSerializer.Serialize(new
                {
                    isApproved = false,
                    rejectionReason = request.Reason,
                    rejectedBy = request.RejectedBy ?? "System",
                    rejectionTimestamp = DateTime.UtcNow
                }))
                .TimeToLive(TimeSpan.FromMinutes(1))
                .Send();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"Rejection message sent for request {request.RequestId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rejection for RequestId: {RequestId}", request.RequestId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Send withdrawal message to the process
    /// </summary>
    [HttpPost("withdraw")]
    public async Task<IActionResult> SendWithdrawal([FromBody] WithdrawRequest request)
    {
        try
        {
            _logger.LogInformation("Sending withdrawal for RequestId: {RequestId} - Reason: {Reason}",
                request.RequestId, request.Reason);

            await _zeebeClient.NewPublishMessageCommand()
                .MessageName("msg_catering_withdraw_create_response")
                .CorrelationKey(request.RequestId)
                .Variables(JsonSerializer.Serialize(new
                {
                    withdrawReason = request.Reason,
                    withdrawnBy = request.WithdrawnBy ?? "Customer",
                    withdrawTimestamp = DateTime.UtcNow
                }))
                .TimeToLive(TimeSpan.FromMinutes(1))
                .Send();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"Withdrawal message sent for request {request.RequestId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send withdrawal for RequestId: {RequestId}", request.RequestId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Get process status
    /// </summary>
    [HttpGet("status/{requestId}")]
    public async Task<IActionResult> GetStatus(string requestId)
    {
        try
        {
            var status = await _cateringService.GetRequestStatusAsync(requestId);

            if (status.Count == 0)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Error = $"No process found for RequestId: {requestId}"
                });
            }

            return Ok(new ApiResponse<Dictionary<string, object>>
            {
                Success = true,
                Data = status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for RequestId: {RequestId}", requestId);
            return StatusCode(500, new ApiResponse<object> { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }
}