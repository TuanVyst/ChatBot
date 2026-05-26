using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
namespace ChatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubjectController : ControllerBase
    {
        private static readonly List<string> Subjects = new List<string>
        {
            "L?p trình C#",
            "C?u trúc d? li?u",
            "Co s? d? li?u",
            "Web Development",
            "AI và Machine Learning"
        };
        [HttpGet]
        public IActionResult GetSubjects()
        {
            return Ok(Subjects);
        }
    }
}
