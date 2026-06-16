using BusinessObject.Entities;
using BusinessObject.Enums;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditSubjectModel : PageModel
{
    private readonly ISubjectService _subjectService;
    private readonly IUniversityService _universityService;
    private readonly IAccountRepository _accountRepository;

    public EditSubjectModel(
        ISubjectService subjectService,
        IUniversityService universityService,
        IAccountRepository accountRepository)
    {
        _subjectService = subjectService;
        _universityService = universityService;
        _accountRepository = accountRepository;
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

        await _subjectService.UpdateSubject(Subject);

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