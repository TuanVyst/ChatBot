using ChatBot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;
using System.Security.Claims;

namespace ChatBot.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    public string? Error { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await _authService.LoginAsync(Input.Email, Input.Password);

        if (!result.Success)
        {
            Error = result.Message;
            return Page();
        }

        if (result.RequireOtp)
        {
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
            return RedirectToPage("/Auth/VerifyOtp");
        }

        var user = result.User;

        if (user != null)
        {
            await SignInWithClaimsAsync(
                user.Id.ToString(),
                user.Email,
                user.FullName ?? Input.Email.Split('@')[0],
                user.Role ?? "Customer");
        }

        if (user?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
            return RedirectToPage("/Admin/Index");

        if (user?.Role?.Equals("Lecture", StringComparison.OrdinalIgnoreCase) == true)
            return RedirectToPage("/Lecturer/Index");

        if (user?.Role?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
        {
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("FullName", user.FullName ?? "");
            return RedirectToPage("/Student/Dashboard");
        }

        return RedirectToPage("/Auth/Login");
    }

    private async Task SignInWithClaimsAsync(string userId, string email, string fullName, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email ?? string.Empty),
            new Claim(ClaimTypes.Name, fullName ?? email ?? string.Empty),
            new Claim(ClaimTypes.Role, role ?? "Customer"),
            new Claim("FullName", fullName ?? string.Empty)
        };

        string scheme = CookieAuthenticationDefaults.AuthenticationScheme;
        if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            scheme = "AdminScheme";
        else if (role.Equals("Lecture", StringComparison.OrdinalIgnoreCase) || role.Equals("Lecturer", StringComparison.OrdinalIgnoreCase))
            scheme = "LectureScheme";
        else if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            scheme = "StudentScheme";

        var identity = new ClaimsIdentity(claims, scheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            scheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }
}