namespace Camonda_hands_on.Workers;
using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers.Varibables;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;


public class CateringWithdrawWorker : BaseWorkerService
{
    private readonly ICateringService _cateringService;

    public CateringWithdrawWorker(
        IZeebeClient zeebeClient,
        ILogger<CateringWithdrawWorker> logger,
        ICateringService cateringService)
        : base(zeebeClient, logger, "CAT_CREATE_WITHDRAW", "catering-withdraw-worker")
    {
        _cateringService = cateringService;
    }

    protected override async Task HandleJob(IJobClient jobClient, IJob job)
    {
        Logger.LogInformation("📝 Processing withdrawal for job: {JobKey}", job.Key);

        try
        {
            var variables = JsonSerializer.Deserialize<CateringJobVariables>(job.Variables);

            if (variables == null || string.IsNullOrEmpty(variables.CreateRequestId))
            {
                throw new Exception("Invalid job variables: createRequestId is required");
            }

            var withdrawReason = variables.WithdrawReason ?? "Withdrawn by user";

            Logger.LogInformation("Withdrawing catering request: {RequestId} - Reason: {Reason}",
                variables.CreateRequestId, withdrawReason);

            await _cateringService.ProcessWithdrawAsync(variables.CreateRequestId, withdrawReason);

            var updatedVariables = new
            {
                status = "WITHDRAWN",
                withdrawnAt = DateTime.UtcNow.ToString("O"),
                withdrawReason = withdrawReason
            };

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(updatedVariables))
                .Send();

            Logger.LogInformation("✅ Completed withdrawal for request: {RequestId}", variables.CreateRequestId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Failed to process withdrawal job {JobKey}", job.Key);

            await jobClient.NewFailCommand(job.Key)
                .Retries(job.Retries - 1)
                .ErrorMessage(ex.Message)
                .Send();
        }
    }
}