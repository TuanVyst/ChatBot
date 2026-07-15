using System.ComponentModel.DataAnnotations;
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
    public InputModel Input { get; set; } = new();

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
            return Page();

        try
        {
            var isValid = await _authService.ValidateAccountAsync(Input.Email);
            if (!isValid)
            {
                Error = $"Email '{Input.Email}' đã tồn tại.";
                return Page();
            }

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