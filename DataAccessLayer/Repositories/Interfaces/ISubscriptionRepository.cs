using BusinessObject.Entities;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<List<SubscriptionPlan>> GetActivePlansAsync();
        Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);
        Task<Subscription?> GetActiveSubscriptionByAccountIdAsync(Guid accountId);
        Task<Subscription> CreateSubscriptionAsync(Subscription subscription);
        Task<PaymentTransaction> CreatePaymentTransactionAsync(PaymentTransaction transaction);
        Task<PaymentTransaction?> GetPaymentByOrderCodeAsync(long orderCode);
        Task UpdatePaymentTransactionAsync(PaymentTransaction transaction);
        Task<List<PaymentTransaction>> GetPaymentHistoryByAccountIdAsync(Guid accountId);
        Task<int> CountTodayQuestionsAsync(Guid accountId);
    }
}
