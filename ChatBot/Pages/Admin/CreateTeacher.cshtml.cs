using System.ComponentModel.DataAnnotations;
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
    public InputModel Teacher { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MaxLength(100, ErrorMessage = "Tên đăng nhập tối đa 100 ký tự")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;
    }

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

        var isValid = await _authService.ValidateAccountAsync(Teacher.Email);

        if (!isValid)
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