using BusinessObject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Student
{
    [Authorize(Roles = "Student")]
    public class PremiumModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;

        public PremiumModel(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        public Subscription? ActiveSubscription { get; set; }
        public List<SubscriptionPlan> AvailablePlans { get; set; } = new();
        public List<PaymentTransaction> PaymentTransactions { get; set; } = new();
        public int RemainingTokens { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var accountId))
            {
                return RedirectToPage("/Auth/Login");
            }

            ActiveSubscription = await _subscriptionService.GetActiveSubscriptionAsync(accountId);
            AvailablePlans = await _subscriptionService.GetAvailablePlansAsync();
            PaymentTransactions = await _subscriptionService.GetPaymentHistoryAsync(accountId);
            RemainingTokens = await _subscriptionService.GetRemainingTokensAsync(accountId);

            return Page();
        }

        public class CreatePaymentRequest
        {
            public int PlanId { get; set; }
        }

        public async Task<IActionResult> OnPostCreateSubscriptionPaymentAsync([FromBody] CreatePaymentRequest request)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var accountId))
                return new JsonResult(new { success = false, errorMessage = "User is not logged in." }) { StatusCode = 401 };

            var returnUrl = Url.Page("/Student/PaymentCallback", null, null, Request.Scheme) ?? "http://localhost:5000/Student/PaymentCallback";
            var cancelUrl = returnUrl;

            var result = await _subscriptionService.CreatePaymentAsync(accountId, request.PlanId, returnUrl, cancelUrl);

            if (!result.Success)
                return new JsonResult(new { success = false, errorMessage = result.Error });

            return new JsonResult(new { 
                success = true, 
                checkoutUrl = result.CheckoutUrl, 
                qrCode = result.QrCode, 
                orderCode = result.OrderCode 
            });
        }

        public async Task<IActionResult> OnGetCheckPaymentStatusAsync(long orderCode)
        {
            var result = await _subscriptionService.HandlePaymentCallbackAsync(orderCode);
            return new JsonResult(new { success = result.Success, error = result.Error });
        }
    }
}
