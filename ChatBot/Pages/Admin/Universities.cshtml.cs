using BusinessObject.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UniversitiesModel : PageModel
{
    private readonly IUniversityService _universityService;

    public UniversitiesModel(IUniversityService universityService)
    {
        _universityService = universityService;
    }

    public IEnumerable<University> Universities { get; set; }
        = new List<University>();

    public async Task OnGetAsync()
    {
        Universities =
            await _universityService.GetUniversities();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _universityService.DeleteUniversity(id);

        TempData["Message"] =
            "University deleted successfully.";

        return RedirectToPage();
    }
}