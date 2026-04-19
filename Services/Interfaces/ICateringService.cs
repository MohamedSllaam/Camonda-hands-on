using Camonda_hands_on.Models.Catering;

namespace Camonda_hands_on.Services.Interfaces;

 
public interface ICateringService
{
    Task<bool> ValidateRequestAsync(string requestId, decimal amount);
    Task ProcessApprovalAsync(string requestId, string approvedBy);
    Task ProcessRejectionAsync(string requestId, string reason);
    Task ProcessWithdrawAsync(string requestId, string reason);
    Task ProcessTimeoutAsync(string requestId);
    Task NotifyCateringProviderAsync(string requestId, string messageType, object data);
    Task<Dictionary<string, object>> GetRequestStatusAsync(string requestId);
    void StoreRequest(string requestId, StartCateringRequest request);
}
