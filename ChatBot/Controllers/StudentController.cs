using ChatBot.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login", "Auth");

            var subjects = await _context.Subjects
                .OrderBy(s => s.Code)
                .ToListAsync();

            var model = new StudentDashboardViewModel
            {
                Subjects = subjects
            };

            return View(model);
        }

        public async Task<IActionResult> SubjectDetail(Guid id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login", "Auth");

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound();

            var documents = await _context.Documents
                .Where(d => d.SubjectId == id)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            var model = new StudentSubjectDetailViewModel
            {
                Subject = subject,
                Documents = documents
            };

            return View(model);
        }

        public async Task<IActionResult> Download(int id)
        {
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

            if (doc == null)
                return NotFound();

            if (!System.IO.File.Exists(doc.FilePath))
                return NotFound("File not found on server.");

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(doc.FilePath, out var contentType))
                contentType = "application/octet-stream";

            var stream = System.IO.File.OpenRead(doc.FilePath);

            return File(stream, contentType, doc.FileName);
        }

        public IActionResult Chat(Guid? subjectId, int? documentId)
        {
            ViewBag.SubjectId = subjectId;
            ViewBag.DocumentId = documentId;

            return View();
        }
    }
}