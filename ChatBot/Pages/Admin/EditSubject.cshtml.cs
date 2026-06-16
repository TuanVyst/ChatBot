using BusinessObject.Entities;
using BusinessObject.Enums;
using ChatBot.Hubs;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditSubjectModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IUniversityService _universityService;
    private readonly IAccountRepository _accountRepository;
    private readonly IHubContext<NotificationHub> _hubContext;

    public EditSubjectModel(
        ISubjectService subjectService,
        IUniversityService universityService,
        IAccountRepository accountRepository,
        IHubContext<NotificationHub> hubContext)
    {
        _subjectService = subjectService;
        _universityService = universityService;
        _accountRepository = accountRepository;
        _hubContext = hubContext;
    }

    [BindProperty]
    public Subject Subject { get; set; } = new();

    public SelectList Universities { get; set; }

    public SelectList Teachers { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var subject = await _subjectService.GetSubjectById(id);

        if (subject == null)
            return NotFound();

        Subject = subject;

        await LoadDropdownsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Subject.University");

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        var oldSubject = await _subjectService.GetSubjectById(Subject.Id.ToString());
        await _subjectService.UpdateSubject(Subject);

        if (oldSubject?.LectureAccountId != Subject.LectureAccountId)
        {
            if (oldSubject?.LectureAccountId != null)
            {
                await _hubContext.Clients.Group(oldSubject.LectureAccountId.ToString())
                    .SendAsync("RefreshData",
                        $"Bạn đã bị xóa khỏi môn học \"{oldSubject.Name}\"");
            }
            if (Subject.LectureAccountId != null)
            {
                await _hubContext.Clients.Group(Subject.LectureAccountId.ToString())
                    .SendAsync("RefreshData",
                        $"Bạn đã được thêm vào môn học \"{Subject.Name}\" ({Subject.Code})");
            }
        }
        else if (oldSubject?.LectureAccountId != null)
        {
            await _hubContext.Clients.Group(oldSubject.LectureAccountId.ToString())
                .SendAsync("RefreshData",
                    $"Thông tin môn học \"{Subject.Name}\" đã được cập nhật");
        }

        return RedirectToPage("Subjects");
    }

    private async Task LoadDropdownsAsync()
    {
        Universities = new SelectList(
            await _universityService.GetUniversities(),
            "Id",
            "Name");

        var teachers =
            (await _accountRepository.GetAllUserInformationsAsync())
            .Where(x => x.Account.Role == RoleEnum.Lecture);

        Teachers = new SelectList(
            teachers,
            "Account_id",
            "Name");
    }
}