using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (User.IsInRole("Lecture"))
            {
                return RedirectToAction("Index", "Lecturer");
            }

            if (User.IsInRole("Student"))
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                    return RedirectToAction("Login", "Auth");
                return RedirectToAction("Dashboard", "Student");
            }

            return RedirectToAction("Login", "Auth");
        }
    }
}
