using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers.Ordering.Variables;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camonda_hands_on.Workers.Ordering;

public class FailPaymentWorker : BaseWorkerService
{
    private readonly IOrderingService _orderingService;

    public FailPaymentWorker(
        IZeebeClient zeebeClient,
        ILogger<FailPaymentWorker> logger,
        IOrderingService orderingService)
        : base(zeebeClient, logger, "fail_payment", "fail-payment-worker")
    {
        _orderingService = orderingService;
    }

    protected override async Task HandleJob(IJobClient jobClient, IJob job)
    {
        Logger.LogInformation("Processing payment failure for job: {JobKey}", job.Key);

        try
        {
            var variables = JsonSerializer.Deserialize<OrderingJobVariables>(job.Variables);

            if (variables == null || string.IsNullOrEmpty(variables.OrderRequestId))
            {
                throw new Exception("Invalid job variables: OrderRequestId is required");
            }

            Logger.LogWarning("Payment failed for Order: {OrderRequestId}. Insufficient balance.",
                variables.OrderRequestId);

            // Process the failed payment
            await _orderingService.ProcessRejectionAsync(variables.OrderRequestId, "Insufficient balance");

            var updatedVariables = new
            {
                orderStatus = "FAILED",
                failureReason = "Insufficient balance",
                failedAt = DateTime.UtcNow.ToString("O")
            };

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(updatedVariables))
                .Send();

            Logger.LogInformation("✅ Payment failure recorded for Order: {OrderRequestId}", variables.OrderRequestId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Failed to process payment failure");
            await jobClient.NewFailCommand(job.Key)
                .Retries(job.Retries - 1)
                .ErrorMessage(ex.Message)
                .Send();
        }
    }
}