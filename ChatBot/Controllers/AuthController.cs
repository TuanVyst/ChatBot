using ChatBot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Interfaces;
using BusinessObject.Dtos.RequestModel;
using BusinessObject.Enums;

using System;
using System.Threading.Tasks;

namespace ChatBot.Controllers
{

    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

    
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // When registering, we will request OTP and store the pending password in TempData
            try
            {
                await _authService.RequestOtpAsync(model.Email);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi gửi mã OTP: " + ex.Message;
                return View(model);
            }

            TempData["PendingEmail"] = model.Email;
            TempData["PendingPassword"] = model.Password;
            TempData["PendingUsername"] = model.Username;
            return RedirectToAction("VerifyOtp");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp(string email)
        {
            // Try to get email from form first, then from TempData
            var pendingEmail = email ?? TempData["PendingEmail"]?.ToString();

            if (string.IsNullOrEmpty(pendingEmail))
                return RedirectToAction("Login");

            // Preserve pending password
            var pendingPassword = TempData["PendingPassword"]?.ToString();

            try
            {
                var msg = await _authService.RequestOtpAsync(pendingEmail);
                TempData["OtpSentMessage"] = msg;
            }
            catch (Exception ex)
            {
                TempData["OtpSentMessage"] = "Lỗi khi gửi mã OTP: " + ex.Message;
            }

            // Re-store values so VerifyOtp can read them
            TempData["PendingEmail"] = pendingEmail;
            if (!string.IsNullOrEmpty(pendingPassword)) TempData["PendingPassword"] = pendingPassword;

            return RedirectToAction("VerifyOtp");
        }

        #region ================= BƯỚC 1: ĐĂNG NHẬP (MẬT KHẨU) =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1. Kiểm tra Email và Mật khẩu
            var result = await _authService.LoginAsync(model.Email, model.Password);

            if (!result.Success)
            {
                ViewBag.Error = result.Message;
                return View(model);
            }

            // 2. If the account requires OTP (last login > 1 day), send OTP and redirect to VerifyOtp
            if (result.RequireOtp)
            {
                try
                {
                    await _authService.RequestOtpAsync(model.Email);
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi gửi mã OTP: " + ex.Message;
                    return View(model);
                }

                // Store PendingEmail for VerifyOtp flow
                TempData["PendingEmail"] = model.Email;
                return RedirectToAction("VerifyOtp");
            }

            // 3. No OTP required: create session and redirect to Home
            var user = result.User;
            if (user != null)
            {
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("FullName", user.FullName ?? model.Email.Split('@')[0]);
                HttpContext.Session.SetString("Role", user.Role ?? "Customer");
            }

            // Redirect admin to Admin dashboard
            if (user != null && !string.IsNullOrEmpty(user.Role) && user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        #endregion


        #region ================= BƯỚC 2: XÁC THỰC OTP =================

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            // Kiểm tra xem có Email từ bước Login truyền sang không
            var pendingEmail = TempData["PendingEmail"]?.ToString();
            // Keep password for the registration flow if exists
            var pendingPassword = TempData["PendingPassword"]?.ToString();
            // Keep username for the registration flow
            var pendingUsername = TempData["PendingUsername"]?.ToString();
            // Keep pending role if admin is creating teacher
            var pendingRole = TempData["PendingRole"]?.ToString();

            // Re-store them so they are available on POST (TempData is one-time)
            if (!string.IsNullOrEmpty(pendingEmail)) TempData["PendingEmail"] = pendingEmail;
            if (!string.IsNullOrEmpty(pendingPassword)) TempData["PendingPassword"] = pendingPassword;
            if (!string.IsNullOrEmpty(pendingUsername)) TempData["PendingUsername"] = pendingUsername;
            if (!string.IsNullOrEmpty(pendingRole)) TempData["PendingRole"] = pendingRole;

            if (string.IsNullOrEmpty(pendingEmail))
            {
                // Nếu ai đó cố tình vào thẳng URL /Auth/VerifyOtp mà chưa nhập mật khẩu, đuổi về trang Login
                return RedirectToAction("Login");
            }

            // Khởi tạo Model và truyền Email vào (Email này sẽ để ở thẻ <input type="hidden"> trên giao diện)
            var model = new VerifyOtpViewModel
            {
                Email = pendingEmail
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // 1. Gọi Service để kiểm tra OTP
                // Map dữ liệu sang DTO mà Service của bạn yêu cầu
                var request = new VerifyOtpRequest
                {
                    Email = model.Email,
                    OtpCode = model.OtpCode,
                    Password = TempData["PendingPassword"]?.ToString(),
                    Username = TempData["PendingUsername"]?.ToString()
                };

                // Check if this verification is for creating an account with a role (e.g., Teacher)
                var pendingRolePost = TempData["PendingRole"]?.ToString();

                if (!string.IsNullOrEmpty(pendingRolePost) && pendingRolePost == "Teacher")
                {
                    var createResult = await _authService.VerifyOtpAndCreateAccountAsync(request, pendingRolePost);
                    if (!createResult.Success)
                    {
                        ViewBag.Error = createResult.Message;
                        return View(model);
                    }

                    TempData["Message"] = createResult.Message;
                    return RedirectToAction("Users", "Admin");
                }

                var result = await _authService.VerifyOtpAndLoginAsync(request);

                // OTP đúng -> Tiến hành lưu Session (sử dụng typed DTO)
                var userResult = result;

                HttpContext.Session.SetString("UserId", userResult.AccountId.ToString());
                HttpContext.Session.SetString("Email", userResult.Email);
                HttpContext.Session.SetString("FullName", userResult.Name ?? model.Email.Split('@')[0]);

                // Set role from returned result
                var roleStr = userResult.Role.ToString();
                HttpContext.Session.SetString("Role", roleStr);

                // Redirect based on role
                if (userResult.Role == BusinessObject.Enums.RoleEnum.Admin)
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // OTP sai hoặc hết hạn -> Báo lỗi trên View
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        #endregion


        #region ================= ĐĂNG XUẤT =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        #endregion
    }
}