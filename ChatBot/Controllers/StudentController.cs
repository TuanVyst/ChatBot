using ChatBot.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObject.Entities;

namespace ChatBot.Controllers
{
    [Authorize(Roles = "Student")]
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

        public async Task<IActionResult> Chat(Guid? subjectId, int? documentId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return RedirectToAction("Login", "Auth");

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!Guid.TryParse(userIdStr, out var studentId))
                return RedirectToAction("Login", "Auth");

            var enrolledSubjectIds = await _context.StudentSubjects
                .Where(ss => ss.AccountId == studentId)
                .Select(ss => ss.SubjectId)
                .ToListAsync();

            var subjects = await _context.Subjects
                .Where(s => enrolledSubjectIds.Contains(s.Id))
                .OrderBy(s => s.Code)
                .ToListAsync();

            var documents = new List<Document>();
            if (subjects.Any())
            {
                var subjectIds = subjects.Select(s => s.Id).ToList();
                documents = await _context.Documents
                    .Include(d => d.Subject)
                    .Include(d => d.Chapter)
                    .Where(d => subjectIds.Contains(d.SubjectId))
                    .Where(d => d.IndexStatus == "Completed")
                    .OrderByDescending(d => d.UploadDate)
                    .ToListAsync();
            }

            var model = new StudentChatViewModel
            {
                Subjects = subjects,
                Documents = documents,
                FullName = HttpContext.Session.GetString("FullName") ?? "Student",
                SelectedSubjectId = subjectId,
                SelectedDocumentId = documentId
            };

            return View(model);
        }
    }
}