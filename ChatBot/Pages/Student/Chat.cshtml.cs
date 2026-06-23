using BusinessObject.Entities;
using BusinessObject.Entities;
using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Pages.Student;

[Authorize(Roles = "Student")]
public class ChatModel : PageModel
{
    private readonly AppDbContext _context;

    public ChatModel(AppDbContext context)
    {
        _context = context;
    }

    public ChatData StudentChat { get; set; } = new();

    public class ChatData
    {
        public string FullName { get; set; } = string.Empty;
        public IReadOnlyList<BusinessObject.Entities.Subject> Subjects { get; set; } = new List<BusinessObject.Entities.Subject>();
        public IReadOnlyList<BusinessObject.Entities.Document> Documents { get; set; } = new List<BusinessObject.Entities.Document>();
        public Guid? SelectedSubjectId { get; set; }
        public int? SelectedDocumentId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid? subjectId, int? documentId)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            return RedirectToPage("/Auth/Login");

        var userIdStr = HttpContext.Session.GetString("UserId");

        if (!Guid.TryParse(userIdStr, out var studentId))
            return RedirectToPage("/Auth/Login");

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

        StudentChat = new ChatData
        {
            Subjects = subjects,
            Documents = documents,
            FullName = HttpContext.Session.GetString("FullName") ?? "Student",
            SelectedSubjectId = subjectId,
            SelectedDocumentId = documentId
        };

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
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
}