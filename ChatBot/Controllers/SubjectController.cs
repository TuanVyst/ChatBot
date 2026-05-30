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
            "PRN222",
            "PRU",
            "SWU",
            "EXE101",
            
        };
        [HttpGet]
        public IActionResult GetSubjects()
        {
            return Ok(Subjects);
        }
    }
}
