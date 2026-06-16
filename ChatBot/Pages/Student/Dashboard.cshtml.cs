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
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            return RedirectToPage("/Auth/Login");

        FullName = HttpContext.Session.GetString("FullName") ?? "Student";

        var subjects = await _context.Subjects
            .OrderBy(s => s.Code)
            .ToListAsync();

        StudentDashboard = new StudentDashboardViewModel
        {
            Subjects = subjects
        };

        return Page();
    }
}