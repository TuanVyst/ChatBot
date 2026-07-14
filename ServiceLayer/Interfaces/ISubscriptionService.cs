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
        Task<int> GetRemainingQuestionsAsync(Guid accountId);
        Task<bool> ConsumeQuestionQuotaAsync(Guid accountId);
        Task<List<PaymentTransaction>> GetPaymentHistoryAsync(Guid accountId);
    }
}
