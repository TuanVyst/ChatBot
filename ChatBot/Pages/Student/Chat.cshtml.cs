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
        public int RemainingQuestions { get; set; }
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

        var remainingQuestions = await _subscriptionService.GetRemainingQuestionsAsync(studentId);

        StudentChat = new ChatData
        {
            Subjects = subjects,
            Documents = documents,
            FullName = HttpContext.Session.GetString("FullName") ?? "Student",
            SelectedSubjectId = subjectId,
            SelectedDocumentId = documentId,
            RemainingQuestions = remainingQuestions
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

        return new JsonResult(new { success = true, answer = result?.Answer, sources = result?.Sources });
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
                .ToList()
        });

        return new JsonResult(new { success = true, history = list });
    }

    public async Task<IActionResult> OnGetSubscriptionPlansAsync()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();
        return new JsonResult(new { success = true, plans = plans.Select(p => new { id = p.Id, name = p.Name, price = p.Price, duration = p.DurationDays, limit = p.DailyQuestionLimit, description = p.Description }) });
    }

    public class CreatePaymentRequest
    {
        public int PlanId { get; set; }
    }

    public async Task<IActionResult> OnPostCreateSubscriptionPaymentAsync([FromBody] CreatePaymentRequest request)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var accountId))
            return new JsonResult(new { success = false, errorMessage = "User is not logged in." }) { StatusCode = 401 };

        // For local testing, we can use a dummy return URL.
        var returnUrl = Url.Page("/Student/PaymentCallback", null, null, Request.Scheme) ?? "http://localhost:5000/Student/PaymentCallback";
        var cancelUrl = returnUrl;

        var result = await _subscriptionService.CreatePaymentAsync(accountId, request.PlanId, returnUrl, cancelUrl);

        if (!result.Success)
            return new JsonResult(new { success = false, errorMessage = result.Error });

        return new JsonResult(new { 
            success = true, 
            checkoutUrl = result.CheckoutUrl, 
            qrCode = result.QrCode, 
            orderCode = result.OrderCode 
        });
    }

    public async Task<IActionResult> OnGetCheckPaymentStatusAsync(long orderCode)
    {
        var result = await _subscriptionService.HandlePaymentCallbackAsync(orderCode);
        return new JsonResult(new { success = result.Success, error = result.Error });
    }
}