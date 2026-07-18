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
public class CreateSubjectModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IUniversityService _universityService;
    private readonly IAccountRepository _accountRepository;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CreateSubjectModel(
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
    public Subject Subject { get; set; } = new Subject();

    public SelectList Teachers { get; set; }

    public async Task OnGetAsync()
    {
        var fpt = (await _universityService.GetUniversities()).FirstOrDefault(u => u.Code == "FPTU");
        if (fpt != null)
        {
            Subject.UniversityId = fpt.Id;
        }
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var fpt = (await _universityService.GetUniversities()).FirstOrDefault(u => u.Code == "FPTU");
        if (fpt != null)
        {
            Subject.UniversityId = fpt.Id;
        }

        ModelState.Remove("Subject.University");

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        await _subjectService.AddSubject(Subject);

        if (Subject.LectureAccountId != null)
        {
            await _hubContext.Clients.Group(Subject.LectureAccountId.ToString())
                .SendAsync("RefreshData",
                    $"Bạn đã được thêm vào môn học \"{Subject.Name}\" ({Subject.Code})");
        }

        return RedirectToPage("Subjects");
    }

    private async Task LoadDropdownsAsync()
    {
        var teachers = (await _accountRepository.GetAllUserInformationsAsync())
            .Where(u => u.Account.Role == RoleEnum.Lecture);

        Teachers = new SelectList(
            teachers,
            "Account_id",
            "Name");
    }

}