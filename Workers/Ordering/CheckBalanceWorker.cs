using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers.Ordering.Variables;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camonda_hands_on.Workers.Ordering;

public class CheckBalanceWorker : BaseWorkerService
{
    private readonly IOrderingService _orderingService;

    public CheckBalanceWorker(
        IZeebeClient zeebeClient,
        ILogger<CheckBalanceWorker> logger,
        IOrderingService orderingService)
        : base(zeebeClient, logger, "check_balance", "check-balance-worker")  // Job type matches BPMN task id
    {
        _orderingService = orderingService;
    }

    protected override async Task HandleJob(IJobClient jobClient, IJob job)
    {
        Logger.LogInformation("Processing balance check for job: {JobKey}", job.Key);

        try
        {
            var variables = JsonSerializer.Deserialize<OrderingJobVariables>(job.Variables);

            if (variables == null || string.IsNullOrEmpty(variables.OrderRequestId))
            {
                throw new Exception("Invalid job variables: OrderRequestId is required");
            }

            Logger.LogInformation("Checking balance for Order: {OrderRequestId}, Amount: {Price:C}",
                variables.OrderRequestId, variables.Price);

            // Call the service to check balance
            var isBalanceEnough = await _orderingService.CheckBalanceAsync(variables.OrderRequestId);

            // Prepare updated variables - IMPORTANT: Use "isEnough" to match BPMN condition
            var updatedVariables = new
            {
                isEnough = isBalanceEnough,  // This matches the BPMN condition expressions
                orderStatus = isBalanceEnough ? "BALANCE_OK" : "INSUFFICIENT_BALANCE",
                checkedAt = DateTime.UtcNow.ToString("O")
            };

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(updatedVariables))
                .Send();

            Logger.LogInformation("✅ Balance check completed. IsEnough: {IsEnough}", isBalanceEnough);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Failed to process balance check");
            await jobClient.NewFailCommand(job.Key)
                .Retries(job.Retries - 1)
                .ErrorMessage(ex.Message)
                .Send();
        }
    }
}