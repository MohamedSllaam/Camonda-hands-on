using Camonda_hands_on.Services.Catering;
using Camonda_hands_on.Services.Interfaces;
using Camonda_hands_on.Services.Ordering;
using Camonda_hands_on.Workers.Catering;
using Microsoft.OpenApi.Models;
using Zeebe.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catering Agreement Orchestrator API",
        Version = "v1",
        Description = "API for orchestrating catering agreements with Camunda 8",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@example.com"
        }
    });
});

builder.Services.AddSingleton<IOrderingService, OrderingService>();

// Register services - Using Singleton for ICateringService
builder.Services.AddSingleton<ICateringService, CateringService>();

// Create Zeebe Client
var zeebeClient = ZeebeClient.Builder()
    .UseGatewayAddress(builder.Configuration["Zeebe:GatewayAddress"] ?? "localhost:26500")
    .UsePlainText()
    .Build();

builder.Services.AddSingleton(zeebeClient);

// Register workers as hosted services
builder.Services.AddHostedService<CateringTimeoutWorker>();
builder.Services.AddHostedService<CateringWithdrawWorker>();
builder.Services.AddHostedService<CateringApproveWorker>();
builder.Services.AddHostedService<CateringRejectWorker>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catering API V1");
        c.RoutePrefix = "swagger"; // This makes Swagger available at /swagger
        // Or use empty string to make it the default page: c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 🔥 DEPLOY THE BPMN FILE 🔥 (Run this after app is built but before starting)
var client = app.Services.GetRequiredService<IZeebeClient>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    // Path to your BPMN file
    string bpmnFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "catering-agreement.bpmn");

    if (File.Exists(bpmnFilePath))
    {
        logger.LogInformation("Deploying BPMN file: {BpmnFile}", bpmnFilePath);

        var deployResponse = await client.NewDeployCommand()
            .AddResourceFile(bpmnFilePath)
            .Send();

        logger.LogInformation("✅ BPMN deployed successfully! Deployment Key: {DeploymentKey}", deployResponse.Key);

        foreach (var process in deployResponse.Processes)
        {
            logger.LogInformation("   Process: {BpmnProcessId} (Version: {Version})",
                process.BpmnProcessId, process.Version);
        }
    }
    else
    {
        logger.LogWarning("⚠️ BPMN file not found at: {BpmnFilePath}", bpmnFilePath);
        logger.LogWarning("   Please place your catering-agreement.bpmn file in the 'Resources' folder");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "❌ Failed to deploy BPMN file");
}

app.Run();