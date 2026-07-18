using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SystemSettingsModel : PageModel
{
    private readonly ISystemSettingService _settingService;

    public SystemSettingsModel(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    [BindProperty]
    public int ChunkSize { get; set; }

    [BindProperty]
    public int ChunkOverlap { get; set; }

    [BindProperty]
    public int TopK { get; set; }

    [BindProperty]
    public string EmbeddingModel { get; set; } = string.Empty;

    [BindProperty]
    public string BackupEmbeddingModel { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var setting = await _settingService.GetSettingAsync();

        ChunkSize = setting.ChunkSize;
        ChunkOverlap = setting.ChunkOverlap;
        TopK = setting.TopK;
        EmbeddingModel = setting.EmbeddingModel;
        BackupEmbeddingModel = setting.BackupEmbeddingModel;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _settingService.UpdateSettingAsync(
            ChunkSize,
            ChunkOverlap,
            TopK,
            EmbeddingModel,
            BackupEmbeddingModel);

        if (result.Success)
            TempData["Message"] = result.Message;
        else
            TempData["Error"] = result.Message;

        return RedirectToPage();
    }
}