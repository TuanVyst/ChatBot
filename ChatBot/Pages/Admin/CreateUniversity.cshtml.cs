using BusinessObject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CreateUniversityModel : PageModel
{
    private readonly IUniversityService _universityService;

    public CreateUniversityModel(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    [BindProperty]
    public University University { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("University.Subjects");

        if (!ModelState.IsValid)
            return Page();

        await _universityService.AddUniversity(University);

        return RedirectToPage("Universities");
    }
}