using BusinessObject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SubjectsModel : PageModel
{
    private readonly ISubjectService _subjectService;

    public SubjectsModel(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();

    public async Task OnGetAsync()
    {
        Subjects = await _subjectService.GetSubjects();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _subjectService.DeleteSubject(id);

        TempData["Message"] = "Subject deleted successfully.";

        return RedirectToPage();
    }
}