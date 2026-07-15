using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessObject.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ChatBot.Pages.Lecturer;

public partial class IndexModel
{
    public async Task<IActionResult> OnPostCreateChapterAsync(
        Guid subjectId,
        string name,
        string? description)
    {
        var (success, msg, _) =
            await _chapterService.CreateChapterAsync(subjectId, name, description);

        if (success)
            TempData["ChapterSuccess"] = msg;
        else
            TempData["ChapterError"] = msg;

        return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId.ToString() });
    }

    public async Task<IActionResult> OnGetChaptersBySubjectAsync(Guid subjectId)
    {
        var chapters = await _chapterService.GetChaptersBySubjectIdAsync(subjectId);
        return new JsonResult(chapters.Select(c => new { id = c.Id, name = c.Name }));
    }
}
