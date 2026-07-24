using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Security.Claims;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly IAccountRepository _accountRepository;

    public UsersModel(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public IEnumerable<UserInformation> Users { get; set; } = new List<UserInformation>();

    public string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? HttpContext.Session.GetString("UserId");

    public async Task OnGetAsync()
    {
        Users = await _accountRepository.GetAllUserInformationsAsync();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                               ?? HttpContext.Session.GetString("UserId");

        if (Guid.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự vô hiệu hóa tài khoản của chính mình.";
            return RedirectToPage();
        }

        var account = await _accountRepository.GetByIdAsync(id);

        if (account != null)
        {
            account.IsActive = !account.IsActive;
            await _accountRepository.UpdateAsync(account);
        }

        return RedirectToPage();
    }
}