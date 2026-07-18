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

    public IActionResult OnGet(int id)
    {
        return RedirectToPage("/Admin/Index");
    }

    public IActionResult OnPost()
    {
        return RedirectToPage("/Admin/Index");
    }
}