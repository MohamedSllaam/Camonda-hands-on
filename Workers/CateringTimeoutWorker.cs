namespace CateringOrchestrator.Workers;

using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers;
using Camonda_hands_on.Workers.Varibables;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;


public class CateringTimeoutWorker : BaseWorkerService
{
    private readonly ICateringService _cateringService;

    public CateringTimeoutWorker(
        IZeebeClient zeebeClient,
        ILogger<CateringTimeoutWorker> logger,
        ICateringService cateringService)
        : base(zeebeClient, logger, "CAT_CREATE_TIMEOUT", "catering-timeout-worker")
    {
        _cateringService = cateringService;
    }

    protected override async Task HandleJob(IJobClient jobClient, IJob job)
    {
        Logger.LogInformation("⏰ Processing timeout for job: {JobKey}", job.Key);

        try
        {
            var variables = JsonSerializer.Deserialize<CateringJobVariables>(job.Variables);

            if (variables == null || string.IsNullOrEmpty(variables.CreateRequestId))
            {
                throw new Exception("Invalid job variables: createRequestId is required");
            }

            Logger.LogWarning("⚠️ Timeout for catering request: {RequestId}", variables.CreateRequestId);

            await _cateringService.ProcessTimeoutAsync(variables.CreateRequestId);

            var updatedVariables = new
            {
                status = "TIMEOUT",
                timeoutTimestamp = DateTime.UtcNow.ToString("O"),
                errorMessage = "No response from catering provider within expected timeframe"
            };

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(updatedVariables))
                .Send();

            Logger.LogInformation("✅ Completed timeout for request: {RequestId}", variables.CreateRequestId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Failed to process timeout job {JobKey}", job.Key);

            await jobClient.NewFailCommand(job.Key)
                .Retries(job.Retries - 1)
                .ErrorMessage(ex.Message)
                .Send();
        }
    }
}