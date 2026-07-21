using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObject.Entities;
using ChatBot.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Lecturer;

public partial class IndexModel
{
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

        var safeFileName = doc.FileName?.Replace("\r", "").Replace("\n", "");
        var cd = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
        cd.SetHttpFileName(safeFileName);
        Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition, cd.ToString());

        var stream = System.IO.File.OpenRead(doc.FilePath);

        return File(stream, contentType);
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
        {
            TempData["UploadSuccess"] = message;

            var subject = await _subjectService.GetSubjectById(subjectId);
            var students = await _subjectService.GetStudentsBySubjectIdAsync(Guid.Parse(subjectId));
            foreach (var student in students)
            {
                await _hubContext.Clients.Group(student.Account_id.ToString())
                    .SendAsync("RefreshData",
                        $"Tài liệu mới \"{file.FileName}\" đã được upload vào môn học \"{subject?.Name ?? ""}\"");
            }
        }
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
