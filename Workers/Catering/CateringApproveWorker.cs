using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers;
using Camonda_hands_on.Workers.Catering.Varibables;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camonda_hands_on.Workers.Catering;

public class CateringApproveWorker : BaseWorkerService
{
    private readonly ICateringService _cateringService;

    public CateringApproveWorker(
        IZeebeClient zeebeClient,
        ILogger<CateringApproveWorker> logger,
        ICateringService cateringService)
        : base(zeebeClient, logger, "CAT_CREATE_APPROVE", "catering-approve-worker")
    {
        _cateringService = cateringService;
    }

    protected override async Task HandleJob(IJobClient jobClient, IJob job)
    {
        Logger.LogInformation("✅ Processing approval for job: {JobKey}", job.Key);

        try
        {
            var variables = JsonSerializer.Deserialize<CateringJobVariables>(job.Variables);

            if (variables == null || string.IsNullOrEmpty(variables.CreateRequestId))
            {
                throw new Exception("Invalid job variables: createRequestId is required");
            }

            var approvedBy = variables.ApprovedBy ?? "System";

            Logger.LogInformation("Approving catering request: {RequestId} - Amount: {Amount:C} - By: {ApprovedBy}",
                variables.CreateRequestId, variables.TotalAmount, approvedBy);

            await _cateringService.ProcessApprovalAsync(variables.CreateRequestId, approvedBy);

            var updatedVariables = new 
            {
                status = "APPROVED",
                approvedAt = DateTime.UtcNow.ToString("O"),
                approvedBy,
                approvalAmount = variables.TotalAmount
            };

            await jobClient.NewCompleteJobCommand(job.Key)
                .Variables(JsonSerializer.Serialize(updatedVariables))
                .Send();

            Logger.LogInformation("✅ Completed approval for request: {RequestId}", variables.CreateRequestId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Failed to process approval job {JobKey}", job.Key);

            await jobClient.NewFailCommand(job.Key)
                .Retries(job.Retries - 1)
                .ErrorMessage(ex.Message)
                .Send();
        }
    }
}