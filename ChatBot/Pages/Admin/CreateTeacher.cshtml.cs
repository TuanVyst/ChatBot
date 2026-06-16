using ChatBot.Models;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CreateTeacherModel : PageModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthService _authService;

    public CreateTeacherModel(
        IAccountRepository accountRepository,
        IAuthService authService)
    {
        _accountRepository = accountRepository;
        _authService = authService;
    }

    [BindProperty]
    public CreateTeacherViewModel Teacher { get; set; } = new();

    public string? Error { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Error = "Vui lòng kiểm tra lại dữ liệu.";
            return Page();
        }

        var existingUser =
            await _accountRepository.GetUserInfoByEmailAsync(
                Teacher.Email);

        if (existingUser != null)
        {
            Error = $"Email '{Teacher.Email}' đã tồn tại.";
            return Page();
        }

        try
        {
            var otpMessage =
                await _authService.RequestOtpAsync(
                    Teacher.Email);

            TempData["AdminPendingUsername"] =
                Teacher.Username;

            TempData["AdminPendingEmail"] =
                Teacher.Email;

            TempData["AdminPendingPassword"] =
                Teacher.Password;

            TempData["OtpSentMessage"] =
                otpMessage;

            return RedirectToPage("AdminVerifyOtp");
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}