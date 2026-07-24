using BusinessObject.Entities;
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

    public StudentDashboardData StudentDashboard { get; set; } = new();

    public class StudentDashboardData
    {
        public IReadOnlyList<BusinessObject.Entities.Subject> Subjects { get; set; } = new List<BusinessObject.Entities.Subject>();
        public int TotalDocuments { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
            return RedirectToPage("/Auth/Login");

        if (!Guid.TryParse(userIdStr, out var studentId))
            return RedirectToPage("/Auth/Login");

        FullName = HttpContext.Session.GetString("FullName") ?? "Student";

        var subjects = await _context.Subjects
            .OrderBy(s => s.Code)
            .ToListAsync();

        StudentDashboard = new StudentDashboardData
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
