using BusinessObject.Dtos.RequestModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;
using System.Text.Json;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AdminVerifyOtpModel : PageModel
{
    private readonly IAuthService _authService;

    public AdminVerifyOtpModel(IAuthService authService)
    {
        _authService = authService;
    }

    public string? PendingEmail { get; set; }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string OtpCode { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        var pendingEmail = TempData["AdminPendingEmail"]?.ToString();
        var pendingPassword = TempData["AdminPendingPassword"]?.ToString();
        var pendingUsername = TempData["AdminPendingUsername"]?.ToString();

        if (string.IsNullOrEmpty(pendingEmail))
            return RedirectToPage("Users");

        TempData["AdminPendingEmail"] = pendingEmail;
        TempData["AdminPendingPassword"] = pendingPassword;
        TempData["AdminPendingUsername"] = pendingUsername;

        PendingEmail = pendingEmail;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var pendingEmail = TempData["AdminPendingEmail"]?.ToString();
        var pendingPassword = TempData["AdminPendingPassword"]?.ToString();
        var pendingUsername = TempData["AdminPendingUsername"]?.ToString();

        if (string.IsNullOrEmpty(pendingEmail) ||
            !string.Equals(pendingEmail, Email, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Message"] = "Pending email mismatch or expired. Please re-initiate Create Teacher.";
            return RedirectToPage("Users");
        }

        var request = new VerifyOtpRequest
        {
            Email = pendingEmail,
            OtpCode = OtpCode,
            Password = pendingPassword,
            Username = pendingUsername
        };

        var createResult = await _authService.VerifyOtpAndCreateAccountAsync(request, "Teacher");

        TempData["Message"] = createResult.Message;

        return RedirectToPage("Users");
    }

    public async Task<IActionResult> OnPostResendOtpAsync([FromBody] JsonElement payload)
    {
        var email = payload.GetProperty("email").GetString();

        if (string.IsNullOrEmpty(email))
            return BadRequest("Email missing");

        await _authService.RequestOtpAsync(email);

        return Content("OTP đã được gửi lại");
    }

    public IActionResult OnGetCancel()
    {
        TempData.Remove("AdminPendingEmail");
        TempData.Remove("AdminPendingPassword");
        TempData.Remove("AdminPendingUsername");

        return RedirectToPage("Users");
    }
}