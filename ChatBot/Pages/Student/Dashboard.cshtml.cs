using ChatBot.Models;
using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Pages.Student;

[Authorize(Roles = "Student")]
public class DashboardModel : PageModel
{
    private readonly AppDbContext _context;
    public string FullName { get; set; } = "Student";

    public DashboardModel(AppDbContext context)
    {
        _context = context;
    }

    public StudentDashboardViewModel StudentDashboard { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
            return RedirectToPage("/Auth/Login");

        if (!Guid.TryParse(userIdStr, out var studentId))
            return RedirectToPage("/Auth/Login");

        FullName = HttpContext.Session.GetString("FullName") ?? "Student";

        var enrolledSubjectIds = await _context.StudentSubjects
            .Where(ss => ss.AccountId == studentId)
            .Select(ss => ss.SubjectId)
            .ToListAsync();

        var subjects = await _context.Subjects
            .Where(s => enrolledSubjectIds.Contains(s.Id))
            .OrderBy(s => s.Code)
            .ToListAsync();

        StudentDashboard = new StudentDashboardViewModel
        {
            Subjects = subjects
        };

        return Page();
    }

    public async Task<IActionResult> OnGetNotificationsAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var studentId))
            return new JsonResult(new { notifications = new object[0] });

        var lastCheckStr = HttpContext.Session.GetString("LastNotificationCheck");
        var lastCheck = string.IsNullOrEmpty(lastCheckStr)
            ? DateTime.UtcNow.AddDays(-7)
            : DateTime.Parse(lastCheckStr);

        var newSubjects = await _context.StudentSubjects
            .Where(ss => ss.AccountId == studentId && ss.EnrolledAt > lastCheck)
            .Include(ss => ss.Subject)
            .Select(ss => new
            {
                type = "enrolled",
                message = "Bạn đã được thêm vào môn học \"" + ss.Subject!.Name + "\"",
                time = ss.EnrolledAt
            })
            .ToListAsync();

        var enrolledIds = await _context.StudentSubjects
            .Where(ss => ss.AccountId == studentId)
            .Select(ss => ss.SubjectId)
            .ToListAsync();

        var newDocs = await _context.Documents
            .Where(d => enrolledIds.Contains(d.SubjectId) && d.UploadDate > lastCheck)
            .Include(d => d.Subject)
            .Select(d => new
            {
                type = "document",
                message = "Tài liệu \"" + d.FileName + "\" đã được upload vào môn học \"" + d.Subject!.Name + "\"",
                time = d.UploadDate
            })
            .ToListAsync();

        var all = newSubjects.Cast<object>().Concat(newDocs.Cast<object>()).ToList();

        HttpContext.Session.SetString("LastNotificationCheck", DateTime.UtcNow.ToString("o"));

        return new JsonResult(new { notifications = all });
    }
}