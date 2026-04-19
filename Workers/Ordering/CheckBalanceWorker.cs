using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Workers.Catering;
using Camonda_hands_on.Workers.Catering.Varibables;
using System.Text.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Camonda_hands_on.Workers.Ordering;

public class CheckBalanceWorker : BaseWorkerService
{
    public CheckBalanceWorker(
      IZeebeClient zeebeClient,
      ILogger<CheckBalanceWorker> logger)
      : base(zeebeClient, logger, "CAT_CREATE_APPROVE", "catering-approve-worker")
    {
       
    }
    protected override Task HandleJob(IJobClient jobClient, IJob job)
    {
        var variables = JsonSerializer.Deserialize<OrderingJobVariables>(job.Variables);

        if (variables == null || string.IsNullOrEmpty(variables.OrderRequestId))
        {
            throw new Exception("Invalid job variables: createRequestId is required");
        }

    }
}
