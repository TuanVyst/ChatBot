using BusinessObject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditUniversityModel : PageModel
{
    private readonly IUniversityService _universityService;

    public EditUniversityModel(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    [BindProperty]
    public University University { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var university =
            await _universityService.GetUniversityById(id);

        if (university == null)
            return NotFound();

        University = university;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("University.Subjects");

        if (!ModelState.IsValid)
            return Page();

        await _universityService.UpdateUniversity(University);

        return RedirectToPage("Universities");
    }
}