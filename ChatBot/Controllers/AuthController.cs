using BusinessObject.Dtos.RequestModel;
using ChatBot.Models;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Interfaces;

namespace ChatBot.Controllers
{
   
        [Route("api/[controller]")]
        [ApiController]
        public class AuthController : Controller
        {

            private readonly IAuthService _authService;

            // Inject Service vào Controller
            public AuthController(IAuthService authService)
            {
                _authService = authService;
            }

        [HttpGet]
        public IActionResult Login()
            [HttpPost("request-otp")]
            public async Task<IActionResult> RequestOtp([FromBody] RequestOtp request)
            {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
                try
                {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(
                model.Email,
                model.Password);

            if (!result.Success)
                    var result = await _authService.RequestOtpAsync(request.Email);
                    return Ok(new { Message = result });
                }
                catch (Exception ex)
                {
                ViewBag.Error = result.Message;
                return View(model);
                    return BadRequest(ex.Message);
                }
            }

            HttpContext.Session.SetString("UserId", result.User!.Id.ToString());
            HttpContext.Session.SetString("FullName", result.User.FullName);
            HttpContext.Session.SetString("Email", result.User.Email);
            HttpContext.Session.SetString("Role", result.User.Role);

            return RedirectToAction("Index", "Home");
            [HttpPost("verify-otp-login")]
            public async Task<IActionResult> VerifyOtpLogin([FromBody] VerifyOtpRequest request)
            {
                try
                {
                    var result = await _authService.VerifyOtpAndLoginAsync(request);
                    return Ok(new { Data = result });
                }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
                catch (Exception ex)
                {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
                    return BadRequest(ex.Message);
                }
            }
        }
    }

