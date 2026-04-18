namespace Camonda_hands_on.Workers.Catering;

using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers;
using Camonda_hands_on.Workers.Catering.Varibables;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;


public class CateringRejectWorker : BaseWorkerService
{
    private readonly ICateringService _cateringService;

    public CateringRejectWorker(
        IZeebeClient zeebeClient,
        ILogger<CateringRejectWorker> logger,
        ICateringService cateringService)
        : base(zeebeClient, logger, "CAT_CREATE_REJECT", "catering-reject-worker")
    {
        _cateringService = cateringService;
    }

    protected override async Task HandleJob(IJobClient jobClient, IJob job)
    {
        Logger.LogInformation("❌ Processing rejection for job: {JobKey}", job.Key);

        try
        {
            var variables = JsonSerializer.Deserialize<CateringJobVariables>(job.Variables);

            if (variables == null || string.IsNullOrEmpty(variables.CreateRequestId))
            {
                throw new Exception("Invalid job variables: createRequestId is required");
            }

            var rejectionReason = variables.RejectionReason ?? "Request rejected by approver";

            Logger.LogWarning("Rejecting catering request: {RequestId} - Reason: {Reason}",
                variables.CreateRequestId, rejectionReason);

            await _cateringService.ProcessRejectionAsync(variables.CreateRequestId, rejectionReason);

            var updatedVariables = new
            {
                status = "REJECTED",
                rejectedAt = DateTime.UtcNow.ToString("O"),
                rejectionReason
            };

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(updatedVariables))
                .Send();

            Logger.LogInformation("✅ Completed rejection for request: {RequestId}", variables.CreateRequestId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Failed to process rejection job {JobKey}", job.Key);

            await jobClient.NewFailCommand(job.Key)
                .Retries(job.Retries - 1)
                .ErrorMessage(ex.Message)
                .Send();
        }
    }
}