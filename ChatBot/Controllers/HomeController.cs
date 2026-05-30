using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
