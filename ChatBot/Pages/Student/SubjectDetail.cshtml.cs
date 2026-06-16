using ChatBot.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Pages.Student;

[Authorize(Roles = "Student")]
public class SubjectDetailModel : PageModel
{
    private readonly AppDbContext _context;

    public SubjectDetailModel(AppDbContext context)
    {
        _context = context;
    }

    public StudentSubjectDetailViewModel SubjectDetail { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            return RedirectToPage("/Auth/Login");

        var subject = await _context.Subjects
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
            return NotFound();

        var documents = await _context.Documents
            .Where(d => d.SubjectId == id)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();

        SubjectDetail = new StudentSubjectDetailViewModel
        {
            Subject = subject,
            Documents = documents
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