using BusinessObject.Entities;

namespace ServiceLayer.Interfaces
{
    public interface ISubscriptionService
    {
        Task<List<SubscriptionPlan>> GetAvailablePlansAsync();
        Task<Subscription?> GetActiveSubscriptionAsync(Guid accountId);
        Task<(bool Success, string? CheckoutUrl, string? QrCode, long? OrderCode, string? Error)> CreatePaymentAsync(
            Guid accountId, int planId, string returnUrl, string cancelUrl);
        Task<(bool Success, string? Error)> HandlePaymentCallbackAsync(long orderCode);
        Task<int> GetRemainingTokensAsync(Guid accountId);
        Task<bool> HasRemainingTokenQuotaAsync(Guid accountId);
        Task<List<PaymentTransaction>> GetPaymentHistoryAsync(Guid accountId);
        Task<List<SubscriptionPlan>> GetAllPlansAsync();
        Task<SubscriptionPlan?> GetPlanByIdAsync(int id);
        Task CreatePlanAsync(SubscriptionPlan plan);
        Task UpdatePlanAsync(SubscriptionPlan plan);
    }
}
