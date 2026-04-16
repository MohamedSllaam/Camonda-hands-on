namespace Camonda_hands_on.Services;

using Camonda_hands_on.Services.Interfaces;
using Zeebe.Client;
using Zeebe.Client.Api.Commands;

public class DeploymentService : IDeploymentService
{
    private readonly IZeebeClient _zeebeClient;
    private readonly ILogger<DeploymentService> _logger;

    public DeploymentService(IZeebeClient zeebeClient, ILogger<DeploymentService> logger)
    {
        _zeebeClient = zeebeClient;
        _logger = logger;
    }

    public async Task<bool> DeployProcessAsync(string bpmnFilePath)
    {
        try
        {
            if (!File.Exists(bpmnFilePath))
            {
                _logger.LogError("BPMN file not found: {FilePath}", bpmnFilePath);
                return false;
            }

            _logger.LogInformation("Deploying BPMN file: {FilePath}", bpmnFilePath);

            // ✅ CORRECT: Chain AddResourceFile before Send()
            var deployResponse = await _zeebeClient.NewDeployCommand()
                .AddResourceFile(bpmnFilePath)
                .Send();

            _logger.LogInformation("✅ Deployment successful! Key: {DeploymentKey}", deployResponse.Key);
            
            if (deployResponse.Processes != null)
            {
                foreach (var process in deployResponse.Processes)
                {
                    _logger.LogInformation("   📦 Process: {ProcessId} (Version: {Version})", 
                        process.BpmnProcessId, process.Version);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to deploy BPMN file: {FilePath}", bpmnFilePath);
            return false;
        }
    }

    public async Task<bool> DeployAllProcessesAsync(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogError("Directory not found: {DirectoryPath}", directoryPath);
                return false;
            }

            // Get all BPMN and DMN files
            var allFiles = Directory.GetFiles(directoryPath, "*.bpmn", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directoryPath, "*.bpmn2", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(directoryPath, "*.dmn", SearchOption.AllDirectories))
                .ToArray();

            if (!allFiles.Any())
            {
                _logger.LogWarning("No BPMN/DMN files found in: {DirectoryPath}", directoryPath);
                return false;
            }

            _logger.LogInformation("Found {Count} file(s) to deploy from {DirectoryPath}", allFiles.Length, directoryPath);

            var successCount = 0;
            var failedFiles = new List<string>();

            // Deploy each file individually for better error tracking
            foreach (var file in allFiles)
            {
                try
                {
                    _logger.LogDebug("Deploying resource: {FileName}", Path.GetFileName(file));

                    // Deploy single file
                    var deployResponse = await _zeebeClient.NewDeployCommand()
                        .AddResourceFile(file)
                        .Send();

                    successCount++;
                    _logger.LogInformation("✅ Deployed: {FileName} (Key: {DeploymentKey})",
                        Path.GetFileName(file), deployResponse.Key);

                    // Log deployed processes from this file
                    if (deployResponse.Processes != null && deployResponse.Processes.Any())
                    {
                        foreach (var process in deployResponse.Processes)
                        {
                            _logger.LogInformation("   📦 Process: {ProcessId} (Version: {Version})",
                                process.BpmnProcessId, process.Version);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedFiles.Add(Path.GetFileName(file));
                    _logger.LogError(ex, "❌ Failed to deploy: {FileName}", Path.GetFileName(file));
                }
            }

            // Summary
            if (failedFiles.Any())
            {
                _logger.LogWarning("Deployment completed with {SuccessCount}/{TotalCount} successes. Failed: {FailedFiles}",
                    successCount, allFiles.Length, string.Join(", ", failedFiles));
                return false;
            }

            _logger.LogInformation("✅ Successfully deployed all {Count} resource(s)", allFiles.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to deploy resources from: {DirectoryPath}", directoryPath);
            return false;
        }
    }
}