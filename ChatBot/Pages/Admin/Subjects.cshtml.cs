using BusinessObject.Entities;
using ChatBot.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SubjectsModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public SubjectsModel(ISubjectService subjectService, IHubContext<NotificationHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
    }

    public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();

    public async Task OnGetAsync()
    {
        Subjects = await _subjectService.GetSubjects();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var subject = await _subjectService.GetSubjectById(id);
        await _subjectService.DeleteSubject(id);

        if (subject?.LectureAccountId != null)
        {
            await _hubContext.Clients.Group(subject.LectureAccountId.ToString())
                .SendAsync("RefreshData",
                    $"Môn học \"{subject.Name}\" đã bị xóa bởi admin");
        }

        TempData["Message"] = "Subject deleted successfully.";

        return RedirectToPage();
    }
}