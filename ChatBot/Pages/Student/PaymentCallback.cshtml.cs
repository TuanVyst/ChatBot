using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Student;

[Authorize(Roles = "Student")]
public class PaymentCallbackModel : PageModel
{
    private readonly ISubscriptionService _subscriptionService;

    public PaymentCallbackModel(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync([FromQuery] long? orderCode, [FromQuery] string? status)
    {
        if (orderCode == null || status != "PAID")
        {
            IsSuccess = false;
            ErrorMessage = status == "CANCELLED" 
                ? "Bạn đã huỷ giao dịch." 
                : "Thanh toán không thành công hoặc không hợp lệ.";
            return Page();
        }

        var result = await _subscriptionService.HandlePaymentCallbackAsync(orderCode.Value);
        
        IsSuccess = result.Success;
        if (!IsSuccess)
        {
            ErrorMessage = result.Error ?? "Có lỗi xảy ra khi xử lý giao dịch của bạn.";
        }

        return Page();
    }
}
