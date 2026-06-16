using BusinessObject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AddStudentToSubjectModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public AddStudentToSubjectModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
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

        TempData["Message"] = message;

        return RedirectToPage("Subjects");
    }
}