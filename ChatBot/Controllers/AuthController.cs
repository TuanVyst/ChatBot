using ChatBot.Models;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

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
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(
                model.Email,
                model.Password);

            if (!result.Success)
            {
                ViewBag.Error = result.Message;
                return View(model);
            }

            HttpContext.Session.SetString("UserId", result.User!.Id.ToString());
            HttpContext.Session.SetString("FullName", result.User.FullName);
            HttpContext.Session.SetString("Email", result.User.Email);
            HttpContext.Session.SetString("Role", result.User.Role);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}