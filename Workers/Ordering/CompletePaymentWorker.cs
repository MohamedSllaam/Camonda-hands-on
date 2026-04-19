using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers.Ordering.Variables;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camonda_hands_on.Workers.Ordering;

public class CompletePaymentWorker : BaseWorkerService
{
    private readonly IOrderingService _orderingService;

    public CompletePaymentWorker(
        IZeebeClient zeebeClient,
        ILogger<CompletePaymentWorker> logger,
        IOrderingService orderingService)
        : base(zeebeClient, logger, "complete_payment", "complete-payment-worker")
    {
        _orderingService = orderingService;
    }

    protected override async Task HandleJob(IJobClient jobClient, IJob job)
    {
        Logger.LogInformation("Processing payment completion for job: {JobKey}", job.Key);

        try
        {
            var variables = JsonSerializer.Deserialize<OrderingJobVariables>(job.Variables);

            if (variables == null || string.IsNullOrEmpty(variables.OrderRequestId))
            {
                throw new Exception("Invalid job variables: OrderRequestId is required");
            }

            Logger.LogInformation("Completing payment for Order: {OrderRequestId}, Amount: {Price:C}",
                variables.OrderRequestId, variables.Price);

            // Process the successful payment
            await _orderingService.ProcessApprovalAsync(variables.OrderRequestId, "System");

            var updatedVariables = new
            {
                orderStatus = "COMPLETED",
                paymentCompletedAt = DateTime.UtcNow.ToString("O"),
                transactionId = Guid.NewGuid().ToString()
            };

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(updatedVariables))
                .Send();

            Logger.LogInformation("✅ Payment completed for Order: {OrderRequestId}", variables.OrderRequestId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Failed to complete payment");
            await jobClient.NewFailCommand(job.Key)
                .Retries(job.Retries - 1)
                .ErrorMessage(ex.Message)
                .Send();
        }
    }
}