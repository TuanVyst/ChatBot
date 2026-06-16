using ChatBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IAuthService _authService;

    public RegisterModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

    public string? Error { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            await _authService.RequestOtpAsync(Input.Email);
        }
        catch (Exception ex)
        {
            Error = "Lỗi khi gửi mã OTP: " + ex.Message;
            return Page();
        }

        TempData["PendingEmail"] = Input.Email;
        TempData["PendingPassword"] = Input.Password;
        TempData["PendingUsername"] = Input.Username;

        return RedirectToPage("/Auth/VerifyOtp");
    }
}