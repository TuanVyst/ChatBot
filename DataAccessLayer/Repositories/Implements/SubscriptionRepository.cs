using BusinessObject.Entities;
using BusinessObject.Enums;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories.Implements
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AppDbContext _context;

        public SubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<SubscriptionPlan>> GetActivePlansAsync()
        {
            return await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
        {
            return await _context.SubscriptionPlans.FindAsync(planId);
        }

        public async Task<Subscription?> GetActiveSubscriptionByAccountIdAsync(Guid accountId)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.AccountId == accountId
                         && s.Status == SubscriptionStatus.Active
                         && s.EndDate > DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<Subscription> CreateSubscriptionAsync(Subscription subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<PaymentTransaction> CreatePaymentTransactionAsync(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<PaymentTransaction?> GetPaymentByOrderCodeAsync(long orderCode)
        {
            return await _context.PaymentTransactions
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
        }

        public async Task UpdatePaymentTransactionAsync(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PaymentTransaction>> GetPaymentHistoryByAccountIdAsync(Guid accountId)
        {
            return await _context.PaymentTransactions
                .Include(p => p.Plan)
                .Where(p => p.AccountId == accountId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountTodayQuestionsAsync(Guid accountId)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var accountIdStr = accountId.ToString();

            return await _context.ChatHistories
                .CountAsync(ch => ch.UserId == accountIdStr
                               && ch.CreatedAt >= todayUtc);
        }

        public async Task<int> SumTodayTokensAsync(Guid accountId)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var accountIdStr = accountId.ToString();

            return await _context.ChatHistories
                .Where(ch => ch.UserId == accountIdStr && ch.CreatedAt >= todayUtc)
                .SumAsync(ch => ch.TotalTokens);
        }

        public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
        {
            return await _context.SubscriptionPlans
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task AddPlanAsync(SubscriptionPlan plan)
        {
            await _context.SubscriptionPlans.AddAsync(plan);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePlanAsync(SubscriptionPlan plan)
        {
            _context.SubscriptionPlans.Update(plan);
            await _context.SaveChangesAsync();
        }
    }
}
