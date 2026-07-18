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

    public IActionResult OnGet()
    {
        return RedirectToPage("/Admin/Index");
    }

    public IActionResult OnPostDelete(int id)
    {
        return RedirectToPage("/Admin/Index");
    }
}