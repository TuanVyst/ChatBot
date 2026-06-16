using BusinessObject.Entities;
using ChatBot.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AddStudentToSubjectModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public AddStudentToSubjectModel(ISubjectService subjectService, IHubContext<NotificationHub> hubContext)
    {
        _subjectService = subjectService;
        _hubContext = hubContext;
    }

    public Subject Subject { get; set; }

    [BindProperty]
    public string SubjectId { get; set; }

    [BindProperty]
    public string Email { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var subject = await _subjectService.GetSubjectById(id);

        if (subject == null)
            return NotFound();

        Subject = subject;
        SubjectId = subject.Id.ToString();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Guid.TryParse(SubjectId, out var subjectGuid))
        {
            TempData["Message"] = "Invalid Subject Id";
            return RedirectToPage("Subjects");
        }

        var (success, message) =
            await _subjectService.AddStudentToSubjectAsync(
                Email.Trim(),
                subjectGuid);

        if (success)
        {
            var subject = await _subjectService.GetSubjectById(SubjectId);
            if (subject?.LectureAccountId != null)
            {
                await _hubContext.Clients.Group(subject.LectureAccountId.ToString())
                    .SendAsync("RefreshData",
                        $"Sinh viên mới ({Email}) đã được thêm vào môn học \"{subject.Name}\"");
            }
        }

        TempData["Message"] = message;

        return RedirectToPage("Subjects");
    }
}