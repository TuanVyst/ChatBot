using System.Security.Claims;
using BusinessObject.Entities;
using ChatBot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Lecturer;

[Authorize(Roles = "Lecture")]
public class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IChapterService _chapterService;
    private readonly IMemoryCache _cache;

    public DashboardViewModel Dashboard { get; set; } = new();

    public IndexModel(
        IDocumentService documentService,
        ISubjectService subjectService,
        IChapterService chapterService,
        IMemoryCache cache)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _chapterService = chapterService;
        _cache = cache;
    }

    public async Task<IActionResult> OnGetAsync(string? subjectName = null, string? chapterId = null)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Auth/Login");

        var subjects = await _subjectService.GetSubjectsByTeacherId(userId);
        var subjectList = subjects.ToList();

        string? selectedSubjectId = subjectName;
        if (!string.IsNullOrEmpty(selectedSubjectId) &&
            !subjectList.Any(s => s.Id.ToString() == selectedSubjectId))
        {
            selectedSubjectId = subjectList.FirstOrDefault()?.Id.ToString();
        }

        var chapters = new List<Chapter>();
        if (!string.IsNullOrEmpty(selectedSubjectId) &&
            Guid.TryParse(selectedSubjectId, out var subjGuid))
        {
            chapters = (await _chapterService.GetChaptersBySubjectIdAsync(subjGuid)).ToList();
        }

        string? selectedChapterId = chapterId;
        if (!string.IsNullOrEmpty(selectedChapterId) &&
            !chapters.Any(c => c.Id.ToString() == selectedChapterId))
        {
            selectedChapterId = null;
        }

        var documents = new List<Document>();

        if (subjectList.Any())
        {
            if (string.IsNullOrEmpty(selectedSubjectId))
            {
                var allDocs = await _documentService.GetDocumentsAsync(null, selectedChapterId);
                var ownedSubjectIds = subjectList.Select(s => s.Id).ToList();
                documents = allDocs.Where(d => ownedSubjectIds.Contains(d.SubjectId)).ToList();
            }
            else
            {
                documents = (await _documentService.GetDocumentsAsync(selectedSubjectId, selectedChapterId)).ToList();
            }
        }

        var pendingCount = documents.Count(d =>
            string.Equals(d.IndexStatus, "Pending", StringComparison.OrdinalIgnoreCase));

        var totalStudents = 0;
        foreach (var subj in subjectList)
        {
            var students = await _subjectService.GetStudentsBySubjectIdAsync(subj.Id);
            totalStudents += students.Count();
        }

        Dashboard = new DashboardViewModel
        {
            Subjects = subjectList,
            Documents = documents,
            SelectedSubjectId = selectedSubjectId,
            Chapters = chapters,
            SelectedChapterId = selectedChapterId,
            PendingCount = pendingCount,
            TotalStudents = totalStudents,
            FullName = User.FindFirstValue(ClaimTypes.Name) ?? "Lecturer"
        };

        return Page();
    }

    public async Task<IActionResult> OnGetStudentsInSubjectAsync(string subjectId)
    {
        if (string.IsNullOrEmpty(subjectId) || !Guid.TryParse(subjectId, out var sid))
            return BadRequest("Invalid subject id");

        var students = await _subjectService.GetStudentsBySubjectIdAsync(sid);

        return new PartialViewResult
        {
            ViewName = "_StudentsList",
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<IEnumerable<UserInformation>>(MetadataProvider, ModelState)
            {
                Model = students
            }
        };
    }

    public async Task<IActionResult> OnGetChaptersBySubjectAsync(Guid subjectId)
    {
        var chapters = await _chapterService.GetChaptersBySubjectIdAsync(subjectId);
        return new JsonResult(chapters.Select(c => new { id = c.Id, name = c.Name }));
    }

    public IActionResult OnGetProgress(int id)
    {
        var progressKey = $"doc_progress_{id}";

        if (_cache.TryGetValue(progressKey, out object? progressObj) &&
            progressObj is int progress)
        {
            return new JsonResult(new { progress });
        }

        return new JsonResult(new { progress = 0 });
    }

    public async Task<IActionResult> OnGetChunksAsync(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc == null) return NotFound();

        var chunkService =
            HttpContext.RequestServices.GetService(typeof(IDocumentChunkService))
            as IDocumentChunkService;

        if (chunkService == null)
            return StatusCode(500, "Chunk service not available");

        var chunks = await chunkService.GetDocumentChunksByDocumentIdAsync(id);

        return new PartialViewResult
        {
            ViewName = "_ChunksPartial",
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<
        Tuple<Document, IEnumerable<DocumentChunk>>>(MetadataProvider, ModelState)
            {
                Model = new Tuple<Document, IEnumerable<DocumentChunk>>(doc, chunks)
            }
        };
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc == null) return NotFound();

        if (!System.IO.File.Exists(doc.FilePath))
            return NotFound("File not found on server.");

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(doc.FilePath, out var contentType))
            contentType = "application/octet-stream";

        var stream = System.IO.File.OpenRead(doc.FilePath);

        return File(stream, contentType, doc.FileName);
    }

    public async Task<IActionResult> OnGetViewOriginalAsync(int id)
    {
        var doc = await _documentService.GetByIdAsync(id);
        if (doc == null) return NotFound();

        if (!System.IO.File.Exists(doc.FilePath))
            return NotFound("File not found on server.");

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(doc.FilePath, out var contentType))
            contentType = "application/octet-stream";

        Response.Headers.Append(
            "Content-Disposition",
            $"inline; filename=\"{doc.FileName}\"");

        var stream = System.IO.File.OpenRead(doc.FilePath);

        return File(stream, contentType);
    }

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

    public async Task<IActionResult> OnPostAddStudentToSubjectAsync(
        string subjectId,
        string email)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subjectId))
        {
            TempData["StudentError"] = "Vui lòng nhập đầy đủ email và môn học.";
            return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId });
        }

        if (!Guid.TryParse(subjectId, out var subjGuid))
        {
            TempData["StudentError"] = "Môn học không hợp lệ.";
            return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId });
        }

        var (success, message) =
            await _subjectService.AddStudentToSubjectAsync(email.Trim(), subjGuid);

        if (success)
            TempData["StudentSuccess"] = message;
        else
            TempData["StudentError"] = message;

        return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId });
    }

    public async Task<IActionResult> OnPostImportStudentsExcelAsync(
        Guid subjectId,
        IFormFile file)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var teacherId))
            return RedirectToPage("/Auth/Login");

        var result =
            await _subjectService.ImportStudentsFromExcelAsync(
                subjectId,
                file,
                teacherId);

        if (result.Success)
            TempData["StudentSuccess"] = result.Message;
        else
            TempData["StudentError"] = result.Message;

        return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId.ToString() });
    }

    public async Task<IActionResult> OnPostRemoveStudentAsync(
        Guid accountId,
        Guid subjectId)
    {
        var (success, message) =
            await _subjectService.RemoveStudentFromSubjectAsync(accountId, subjectId);

        return new JsonResult(new { success, message });
    }

    public async Task<IActionResult> OnPostUploadAsync(
        IFormFile file,
        string subjectName,
        string chapterId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Auth/Login");

        var subjects = await _subjectService.GetSubjectsByTeacherId(userId);

        var subjectId = subjectName;

        if (string.IsNullOrEmpty(subjectId) ||
            !subjects.Any(s => s.Id.ToString() == subjectId))
        {
            TempData["UploadError"] = "Unauthorized subject";
            return RedirectToPage("/Lecturer/Index");
        }

        var (success, message, _) =
            await _documentService.UploadDocumentAsync(file, subjectId, chapterId);

        if (success)
            TempData["UploadSuccess"] = message;
        else
            TempData["UploadError"] = message;

        return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId });
    }

    public async Task<IActionResult> OnPostReindexAsync(
        int id,
        string? subjectName = null)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Auth/Login");

        if (!string.IsNullOrEmpty(subjectName))
        {
            var subjects = await _subjectService.GetSubjectsByTeacherId(userId);

            if (!subjects.Any(s => s.Id.ToString() == subjectName))
            {
                TempData["ListError"] = "Unauthorized subject";
                return RedirectToPage("/Lecturer/Index");
            }
        }

        var (success, message) = await _documentService.ReindexDocumentAsync(id);

        if (success)
            TempData["ListSuccess"] = message;
        else
            TempData["ListError"] = message;

        return RedirectToPage("/Lecturer/Index", new { subjectName });
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(
        int id,
        string? subjectName = null)
    {
        var (success, message) = await _documentService.DeleteDocumentAsync(id);

        if (success)
            TempData["ListSuccess"] = message;
        else
            TempData["ListError"] = message;

        return RedirectToPage("/Lecturer/Index", new { subjectName });
    }
}