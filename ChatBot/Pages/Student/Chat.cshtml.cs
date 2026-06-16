using BusinessObject.Entities;
using ChatBot.Models;
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

    public StudentChatViewModel StudentChat { get; set; } = new();

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

        StudentChat = new StudentChatViewModel
        {
            Subjects = subjects,
            Documents = documents,
            FullName = HttpContext.Session.GetString("FullName") ?? "Student",
            SelectedSubjectId = subjectId,
            SelectedDocumentId = documentId
        };

        return Page();
    }
}