using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
namespace ChatBot.Controllers
{
    public class SubjectController : Controller
    {
        private static readonly List<string> Subjects = new List<string>
        {
            "PRN222",
            "PRU",
            "SWU",
            "EXE101",
            
        };
        [HttpGet]
        public IActionResult GetSubjects()
        {
            return View(Subjects);
        }
    }
}
