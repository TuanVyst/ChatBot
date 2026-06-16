using BusinessObject.Dtos.RequestModel;
using BusinessObject.Enums;
using ChatBot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;
using System.Security.Claims;

namespace ChatBot.Pages.Auth;

public class VerifyOtpModel : PageModel
{
    private readonly IAuthService _authService;

    public VerifyOtpModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public VerifyOtpViewModel Input { get; set; } = new();

    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        var pendingEmail = TempData["PendingEmail"]?.ToString();
        var pendingPassword = TempData["PendingPassword"]?.ToString();
        var pendingUsername = TempData["PendingUsername"]?.ToString();
        var pendingRole = TempData["PendingRole"]?.ToString();

        if (!string.IsNullOrEmpty(pendingEmail))
            TempData["PendingEmail"] = pendingEmail;

        if (!string.IsNullOrEmpty(pendingPassword))
            TempData["PendingPassword"] = pendingPassword;

        if (!string.IsNullOrEmpty(pendingUsername))
            TempData["PendingUsername"] = pendingUsername;

        if (!string.IsNullOrEmpty(pendingRole))
            TempData["PendingRole"] = pendingRole;

        if (string.IsNullOrEmpty(pendingEmail))
            return RedirectToPage("/Auth/Login");

        Input = new VerifyOtpViewModel
        {
            Email = pendingEmail
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var request = new VerifyOtpRequest
            {
                Email = Input.Email,
                OtpCode = Input.OtpCode,
                Password = TempData["PendingPassword"]?.ToString(),
                Username = TempData["PendingUsername"]?.ToString()
            };

            var pendingRole = TempData["PendingRole"]?.ToString();

            if (!string.IsNullOrEmpty(pendingRole) && pendingRole == "Teacher")
            {
                var createResult =
                    await _authService.VerifyOtpAndCreateAccountAsync(
                        request,
                        pendingRole);

                if (!createResult.Success)
                {
                    Error = createResult.Message;
                    return Page();
                }

                TempData["Message"] = createResult.Message;
                return Redirect("/Admin/Users");
            }

            var result = await _authService.VerifyOtpAndLoginAsync(request);

            await SignInWithClaimsAsync(
                result.AccountId.ToString(),
                result.Email,
                result.Name ?? Input.Email.Split('@')[0],
                result.Role.ToString());

            if (result.Role == RoleEnum.Admin)
                return Redirect("/Admin");

            if (result.Role == RoleEnum.Lecture)
                return Redirect("/Lecturer");

            HttpContext.Session.SetString("UserId", result.AccountId.ToString());
            HttpContext.Session.SetString("FullName", result.Name ?? "");

            return Redirect("/Student/Dashboard");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostResendOtpAsync(string email)
    {
        var pendingEmail = email ?? TempData["PendingEmail"]?.ToString();

        if (string.IsNullOrEmpty(pendingEmail))
            return RedirectToPage("/Auth/Login");

        var pendingPassword = TempData["PendingPassword"]?.ToString();
        var pendingUsername = TempData["PendingUsername"]?.ToString();

        try
        {
            var msg = await _authService.RequestOtpAsync(pendingEmail);
            TempData["OtpSentMessage"] = msg;
        }
        catch (Exception ex)
        {
            TempData["OtpSentMessage"] = "Lỗi khi gửi mã OTP: " + ex.Message;
        }

        TempData["PendingEmail"] = pendingEmail;

        if (!string.IsNullOrEmpty(pendingPassword))
            TempData["PendingPassword"] = pendingPassword;

        if (!string.IsNullOrEmpty(pendingUsername))
            TempData["PendingUsername"] = pendingUsername;

        return RedirectToPage("/Auth/VerifyOtp");
    }

    private async Task SignInWithClaimsAsync(
        string userId,
        string email,
        string fullName,
        string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email ?? string.Empty),
            new Claim(ClaimTypes.Name, fullName ?? email ?? string.Empty),
            new Claim(ClaimTypes.Role, role ?? "Customer"),
            new Claim("FullName", fullName ?? string.Empty)
        };

        var identity =
            new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }
}