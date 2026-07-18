using BusinessObject.Entities;
using BusinessObject.Enums;
using DataAccessLayer.Repositories.Interfaces;
using ServiceLayer.Interfaces;
using PayOS;

namespace ServiceLayer.Implements
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _repository;
        private readonly PayOSClient _payOSClient;
        private const int FREE_DAILY_TOKEN_LIMIT = 5000;

        public SubscriptionService(ISubscriptionRepository repository, PayOSClient payOSClient)
        {
            _repository = repository;
            _payOSClient = payOSClient;
        }

        public async Task<List<SubscriptionPlan>> GetAvailablePlansAsync()
        {
            return await _repository.GetActivePlansAsync();
        }

        public async Task<Subscription?> GetActiveSubscriptionAsync(Guid accountId)
        {
            return await _repository.GetActiveSubscriptionByAccountIdAsync(accountId);
        }

        public async Task<(bool Success, string? CheckoutUrl, string? QrCode, long? OrderCode, string? Error)> CreatePaymentAsync(
            Guid accountId, int planId, string returnUrl, string cancelUrl)
        {
            try
            {
                var plan = await _repository.GetPlanByIdAsync(planId);
                if (plan == null || !plan.IsActive)
                    return (false, null, null, null, "Gói subscription không tồn tại hoặc đã ngừng.");

                // Generate unique order code using timestamp
                long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    AccountId = accountId,
                    PlanId = planId,
                    OrderCode = orderCode,
                    Amount = plan.Price,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.CreatePaymentTransactionAsync(transaction);

                // Create PayOS payment link using v2 API
                var paymentRequest = new PayOS.Models.V2.PaymentRequests.CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = plan.Price,
                    Description = $"Dang ky {plan.Name}",
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                var createPaymentResult = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);

                // Update transaction with PayOS info
                transaction.CheckoutUrl = createPaymentResult.CheckoutUrl;
                transaction.QrCode = createPaymentResult.QrCode;
                await _repository.UpdatePaymentTransactionAsync(transaction);

                return (true, createPaymentResult.CheckoutUrl, createPaymentResult.QrCode, orderCode, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, null, $"Lỗi tạo thanh toán: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Error)> HandlePaymentCallbackAsync(long orderCode)
        {
            try
            {
                var transaction = await _repository.GetPaymentByOrderCodeAsync(orderCode);
                if (transaction == null)
                    return (false, "Không tìm thấy giao dịch.");

                if (transaction.Status == PaymentStatus.Paid)
                    return (true, null); // Already processed

                // Verify payment with PayOS
                var paymentInfo = await _payOSClient.PaymentRequests.GetAsync(orderCode.ToString());

                if (paymentInfo.Status.ToString().ToUpper() != "PAID")
                    return (false, $"Giao dịch chưa thanh toán. Trạng thái: {paymentInfo.Status}");

                // Update transaction
                transaction.Status = PaymentStatus.Paid;
                transaction.PaidAt = DateTime.UtcNow;
                transaction.PayOSTransactionId = paymentInfo.Id;
                await _repository.UpdatePaymentTransactionAsync(transaction);

                // Create subscription
                var plan = transaction.Plan ?? await _repository.GetPlanByIdAsync(transaction.PlanId);
                if (plan == null)
                    return (false, "Không tìm thấy gói subscription.");

                var now = DateTime.UtcNow;
                var subscription = new Subscription
                {
                    AccountId = transaction.AccountId,
                    PlanId = transaction.PlanId,
                    StartDate = now,
                    EndDate = now.AddDays(plan.DurationDays),
                    Status = SubscriptionStatus.Active,
                    CreatedAt = now
                };

                await _repository.CreateSubscriptionAsync(subscription);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xử lý callback: {ex.Message}");
            }
        }

        public async Task<int> GetRemainingTokensAsync(Guid accountId)
        {
            var todayTokens = await _repository.SumTodayTokensAsync(accountId);
            var activeSub = await _repository.GetActiveSubscriptionByAccountIdAsync(accountId);

            int dailyLimit = activeSub?.Plan?.DailyTokenLimit ?? FREE_DAILY_TOKEN_LIMIT;
            int remaining = dailyLimit - todayTokens;

            return Math.Max(0, remaining);
        }

        public async Task<bool> HasRemainingTokenQuotaAsync(Guid accountId)
        {
            var remaining = await GetRemainingTokensAsync(accountId);
            return remaining > 0;
        }

        public async Task<List<PaymentTransaction>> GetPaymentHistoryAsync(Guid accountId)
        {
            return await _repository.GetPaymentHistoryByAccountIdAsync(accountId);
        }

        public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
        {
            return await _repository.GetAllPlansAsync();
        }

        public async Task<SubscriptionPlan?> GetPlanByIdAsync(int id)
        {
            return await _repository.GetPlanByIdAsync(id);
        }

        public async Task CreatePlanAsync(SubscriptionPlan plan)
        {
            await _repository.AddPlanAsync(plan);
        }

        public async Task UpdatePlanAsync(SubscriptionPlan plan)
        {
            await _repository.UpdatePlanAsync(plan);
        }
    }
}
