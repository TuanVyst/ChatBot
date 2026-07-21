using BusinessObject.Entities;
using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Student;

[Authorize(Roles = "Student")]
public class ChatModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IRagService _ragService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ISubscriptionService _subscriptionService;

    public ChatModel(AppDbContext context, IRagService ragService, IChatHistoryService chatHistoryService, ISubscriptionService subscriptionService)
    {
        _context = context;
        _ragService = ragService;
        _chatHistoryService = chatHistoryService;
        _subscriptionService = subscriptionService;
    }

    public ChatData StudentChat { get; set; } = new();

    public class ChatData
    {
        public string FullName { get; set; } = string.Empty;
        public IReadOnlyList<BusinessObject.Entities.Subject> Subjects { get; set; } = new List<BusinessObject.Entities.Subject>();
        public IReadOnlyList<BusinessObject.Entities.Document> Documents { get; set; } = new List<BusinessObject.Entities.Document>();
        public Guid? SelectedSubjectId { get; set; }
        public int? SelectedDocumentId { get; set; }
        public int RemainingTokens { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid? subjectId, int? documentId)
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            return RedirectToPage("/Auth/Login");

        var userIdStr = HttpContext.Session.GetString("UserId");

        if (!Guid.TryParse(userIdStr, out var studentId))
            return RedirectToPage("/Auth/Login");

        var enrolledSubjectIds = await _context.StudentSubjects
            .Where(ss => ss.AccountId == studentId)
            .Select(ss => ss.SubjectId)
            .ToListAsync();

        var subjects = await _context.Subjects
            .Where(s => enrolledSubjectIds.Contains(s.Id))
            .OrderBy(s => s.Code)
            .ToListAsync();

        var documents = new List<Document>();

        if (subjects.Any())
        {
            var subjectIds = subjects.Select(s => s.Id).ToList();

            documents = await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.Chapter)
                .Where(d => subjectIds.Contains(d.SubjectId))
                .Where(d => d.IndexStatus == "Completed")
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        var remainingTokens = await _subscriptionService.GetRemainingTokensAsync(studentId);

        StudentChat = new ChatData
        {
            Subjects = subjects,
            Documents = documents,
            FullName = HttpContext.Session.GetString("FullName") ?? "Student",
            SelectedSubjectId = subjectId,
            SelectedDocumentId = documentId,
            RemainingTokens = remainingTokens
        };

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null)
            return NotFound();

        if (!System.IO.File.Exists(doc.FilePath))
            return NotFound("File not found on server.");

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(doc.FilePath, out var contentType))
            contentType = "application/octet-stream";

        var stream = System.IO.File.OpenRead(doc.FilePath);

        return File(stream, contentType, doc.FileName);
    }

    public class AskRequest
    {
        public string Question { get; set; } = string.Empty;
        public Guid? SubjectId { get; set; }
        public int? DocumentId { get; set; }
    }

    public async Task<IActionResult> OnPostAskAsync([FromBody] AskRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { success = false, errorMessage = "Question is required." });

        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, errorMessage = "User is not logged in." }) { StatusCode = 401 };

        var (success, result, error) = await _ragService.AskAsync(
            request.Question,
            request.SubjectId,
            chapterId: null,
            documentId: request.DocumentId,
            userId: userId);

        if (!success)
            return new JsonResult(new { success = false, errorMessage = error });

        var remainingTokens = await _subscriptionService.GetRemainingTokensAsync(Guid.Parse(userId));

        return new JsonResult(new
        {
            success = true,
            answer = result?.Answer,
            sources = result?.Sources,
            remainingTokens = remainingTokens,
            chunkSources = result?.RetrievedChunks
                .GroupBy(c => c.Id)
                .Select(g => g.First())
                .Select(c => new
                {
                    documentId = c.DocumentId,
                    fileName = c.Document?.FileName ?? "",
                    chunkId = c.Id,
                    chunkOrder = c.ChunkOrder,
                    content = c.Content
                }).ToList()
        });
    }

    public async Task<IActionResult> OnGetHistoryAsync(Guid? subjectId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, errorMessage = "User is not logged in." }) { StatusCode = 401 };

        var (success, history, error) = await _chatHistoryService.GetHistoryAsync(
            userId, subjectId, chapterId: null, take: 50);

        if (!success || history == null)
            return new JsonResult(new { success = false, errorMessage = error });

        var list = history.Select(h => new
        {
            id = h.Id,
            question = h.Question,
            answer = h.Answer,
            time = h.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy h:mm tt"),
            sources = h.Sources
                .Where(s => s.DocumentChunk?.Document != null)
                .Select(s => s.DocumentChunk!.Document!.FileName)
                .Distinct()
                .ToList(),
            chunkSources = h.Sources
                .Where(s => s.DocumentChunk?.Document != null)
                .GroupBy(s => s.DocumentChunkId)
                .Select(g => g.First())
                .Select(s => new
                {
                    documentId = s.DocumentChunk!.DocumentId,
                    fileName = s.DocumentChunk.Document!.FileName,
                    chunkId = s.DocumentChunkId,
                    chunkOrder = s.DocumentChunk.ChunkOrder,
                    content = s.DocumentChunk.Content
                })
                .ToList()
        });

        return new JsonResult(new { success = true, history = list });
    }

    public async Task<IActionResult> OnGetChunksAsync(int id)
    {
        var doc = await _context.Documents
            .Include(d => d.Subject)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null) return NotFound();

        var chunkService = HttpContext.RequestServices.GetService(typeof(IDocumentChunkService)) as IDocumentChunkService;
        if (chunkService == null)
            return StatusCode(500, "Chunk service not available");

        var chunks = await chunkService.GetDocumentChunksByDocumentIdAsync(id);

        return new PartialViewResult
        {
            ViewName = "~/Pages/Lecturer/_ChunksPartial.cshtml",
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<
                Tuple<Document, IEnumerable<DocumentChunk>>>(MetadataProvider, ModelState)
            {
                Model = new Tuple<Document, IEnumerable<DocumentChunk>>(doc, chunks)
            }
        };
    }

    public async Task<IActionResult> OnGetViewOriginalAsync(int id)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
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
}