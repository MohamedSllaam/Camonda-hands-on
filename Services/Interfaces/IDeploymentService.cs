namespace Camonda_hands_on.Services.Interfaces
{
    public interface IDeploymentService
    {
        Task<bool> DeployProcessAsync(string bpmnFilePath);
        Task<bool> DeployAllProcessesAsync(string directoryPath);
    }
}
