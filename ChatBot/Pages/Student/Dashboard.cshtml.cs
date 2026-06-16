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

        var unreadNotifications = await _context.StudentNotifications
            .Where(n => n.AccountId == studentId && !n.IsRead)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();

        var notifications = unreadNotifications
            .Select(n => new
            {
                id = n.Id,
                type = n.Type,
                message = n.Message,
                time = n.CreatedAt
            })
            .ToList();

        if (unreadNotifications.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _context.SaveChangesAsync();
        }

        return new JsonResult(new { notifications });
    }
}
