using ChatBot.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Interfaces;
using BusinessObject.Dtos.RequestModel;
using Microsoft.AspNetCore.Http;// Đường dẫn đến VerifyOtpRequest của bạn
using System;
using System.Threading.Tasks;

namespace ChatBot.Controllers
{
    // Đã xóa [ApiController] và [Route("api/...")] vì đây là MVC thuần
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
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

            // 2. NẾU ĐÚNG MẬT KHẨU -> Tự động gọi hàm gửi OTP
            try
            {
                await _authService.RequestOtpAsync(model.Email);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi gửi mã OTP: " + ex.Message;
                return View(model);
            }

            // 3. Lưu Email tạm thời để chuyển sang trang nhập OTP
            // Sử dụng TempData để truyền dữ liệu giữa 2 Action (Login -> VerifyOtp)
            TempData["PendingEmail"] = model.Email;

            // 4. Chuyển hướng sang trang nhập OTP
            return RedirectToAction("VerifyOtp");
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

            // Re-store them so they are available on POST (TempData is one-time)
            if (!string.IsNullOrEmpty(pendingEmail)) TempData["PendingEmail"] = pendingEmail;
            if (!string.IsNullOrEmpty(pendingPassword)) TempData["PendingPassword"] = pendingPassword;

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
                    Password = TempData["PendingPassword"]?.ToString()
                };

                var result = await _authService.VerifyOtpAndLoginAsync(request);

                // 2. OTP đúng -> Tiến hành lưu Session (sử dụng typed DTO)
                // Ensure we have the typed result
                var userResult = result;

                // Use the session extension methods via the Microsoft.AspNetCore.Http namespace
                HttpContext.Session.SetString("UserId", userResult.AccountId.ToString());
                HttpContext.Session.SetString("Email", userResult.Email);
                HttpContext.Session.SetString("FullName", userResult.Name ?? model.Email.Split('@')[0]);
                HttpContext.Session.SetString("Role", "Customer");

                // 3. Xong! Đăng nhập thành công, vào trang chủ
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