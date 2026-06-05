using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
namespace ChatBot.Controllers
{
    public class SubjectController : Controller
    {
        private readonly ServiceLayer.Interfaces.ISubjectService _subjectService;

        public SubjectController(ServiceLayer.Interfaces.ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        private static readonly List<string> Subjects = new List<string>
        {
            "PRN222",
            "PRU",
            "SWU",
            "EXE101",
        };
     
        public IActionResult GetSubjects()
        {
            return View(Subjects);
        }

        [HttpPost]
        public async Task<IActionResult> AddStudent(string emailOrUsername, System.Guid subjectId)
        {
            var (success, message) = await _subjectService.AddStudentToSubjectAsync(emailOrUsername, subjectId);
            if (success)
            {
                return Ok(new { message });
            }
            return BadRequest(new { message });
        }
    }
}
