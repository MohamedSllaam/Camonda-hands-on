using Camonda_hands_on.Models.Catering;
using Camonda_hands_on.Models.Ordering;

namespace Camonda_hands_on.Services.Interfaces
{
    public interface IOrderingService
    {
        Task<bool> ValidateOrderAsync(string orderRequestId, decimal amount);
        Task ProcessApprovalAsync(string orderRequestId, string approvedBy);
        Task ProcessRejectionAsync(string orderRequestId, string reason);
        Task ProcessWithdrawAsync(string orderRequestId, string reason);
        Task ProcessTimeoutAsync(string orderRequestId);
        Task NotifyProviderAsync(string orderRequestId, string messageType, object data);
        Task<Dictionary<string, object>> GetRequestStatusAsync(string orderRequestId);
        void StoreRequest(string orderRequestId, CreateOrderRequest request);

    }
}
