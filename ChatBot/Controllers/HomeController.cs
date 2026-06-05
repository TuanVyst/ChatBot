using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ChatBot.Models;
using ServiceLayer.Interfaces;

namespace ChatBot.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly ServiceLayer.Interfaces.IChapterService _chapterService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public HomeController(
            IDocumentService documentService, 
            ISubjectService subjectService,
            ServiceLayer.Interfaces.IChapterService chapterService,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _cache = cache;
        }

        // View document chunks (for verifying embeddings/content)
        public async Task<IActionResult> Chunks(int id)
        {
            var doc = await _documentService.GetByIdAsync(id);
            if (doc == null) return NotFound();

            // Use document chunk service from DI by resolving (we don't have field), use HttpContext.RequestServices
            var chunkService = HttpContext.RequestServices.GetService(typeof(ServiceLayer.Implements.DocumentChunkService)) as ServiceLayer.Implements.DocumentChunkService;
            if (chunkService == null)
            {
                // Try using interface
                var chunkServiceIface = HttpContext.RequestServices.GetService(typeof(ServiceLayer.Interfaces.IDocumentChunkService)) as ServiceLayer.Interfaces.IDocumentChunkService;
                if (chunkServiceIface == null) return StatusCode(500, "Chunk service not available");
                var chunks = await chunkServiceIface.GetDocumentChunksByDocumentIdAsync(id);
                return View("Chunks", new Tuple<BusinessObject.Entities.Document, IEnumerable<BusinessObject.Entities.DocumentChunk>>(doc, chunks));
            }

            var cks = await chunkService.GetDocumentChunksByDocumentIdAsync(id);
            return View("Chunks", new Tuple<BusinessObject.Entities.Document, IEnumerable<BusinessObject.Entities.DocumentChunk>>(doc, cks));
        }

        // Download / Preview file
        public async Task<IActionResult> Download(int id)
        {
            var doc = await _documentService.GetByIdAsync(id);
            if (doc == null) return NotFound();

            if (!System.IO.File.Exists(doc.FilePath))
                return NotFound("File not found on server.");

            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(doc.FilePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            var fileName = doc.FileName;
            var stream = System.IO.File.OpenRead(doc.FilePath);

            return File(stream, contentType, fileName);
        }

       
        public async Task<IActionResult> Index(string? subjectName = null, int? chapterId = null, string? message = null, string? error = null)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var subjects = (await _subjectService.GetSubjectsByTeacherId(userId)).ToList();

            if (!string.IsNullOrEmpty(subjectName) && !subjects.Any(s => s.Name == subjectName))
            {
                subjectName = subjects.FirstOrDefault()?.Name; 
            }

            if (string.IsNullOrEmpty(subjectName) && subjects.Any())
            {
                subjectName = subjects.First().Name;
            }

            var selectedSubject = subjects.FirstOrDefault(s => s.Name == subjectName);
            var chapters = selectedSubject != null 
                ? (await _chapterService.GetChaptersBySubjectIdAsync(selectedSubject.Id)).ToList() 
                : new List<BusinessObject.Entities.Chapter>();

            var documents = await _documentService.GetDocumentsAsync(subjectName ?? string.Empty, chapterId);
            var pendingCount = documents.Count(d => string.Equals(d.IndexStatus, "Pending", StringComparison.OrdinalIgnoreCase));
            
            var model = new DashboardViewModel
            {
                Subjects = subjects,
                Documents = documents.ToList(),
                SelectedSubject = subjectName,
                Chapters = chapters,
                SelectedChapterId = chapterId,
                PendingCount = pendingCount,
                Message = message,
                Error = error,
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult GetProgress(int id)
        {
            var progressKey = $"doc_progress_{id}";
            if (_cache.TryGetValue(progressKey, out object progressObj) && progressObj is int progress)
            {
                return Json(new { progress });
            }
            return Json(new { progress = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChapter(Guid subjectId, string name, string? description, string subjectName)
        {
            var (success, msg, _) = await _chapterService.CreateChapterAsync(subjectId, name, description);
            if (success)
            {
                return RedirectToAction(nameof(Index), new { message = msg, subjectName = subjectName });
            }
            return RedirectToAction(nameof(Index), new { error = msg, subjectName = subjectName });
        }

      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string subjectName, int? chapterId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return RedirectToAction("Login", "Auth");
            var subjects = await _subjectService.GetSubjectsByTeacherId(userId);
            if (!subjects.Any(s => s.Name == subjectName)) return RedirectToAction(nameof(Index), new { error = "Unauthorized subject" });

            var (success, message, _) = await _documentService.UploadDocumentAsync(file, subjectName, chapterId);

            if (success)
            {
                return RedirectToAction(nameof(Index), new { message });
            }

            return RedirectToAction(nameof(Index), new { error = message });
        }

      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reindex(int id, string? subjectName = null)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return RedirectToAction("Login", "Auth");
            
            if (!string.IsNullOrEmpty(subjectName))
            {
                var subjects = await _subjectService.GetSubjectsByTeacherId(userId);
                if (!subjects.Any(s => s.Name == subjectName)) return RedirectToAction(nameof(Index), new { error = "Unauthorized subject" });
            }

            var (success, message) = await _documentService.ReindexDocumentAsync(id);

            if (success)
            {
                return RedirectToAction(nameof(Index), new { message, subjectName });
            }

            return RedirectToAction(nameof(Index), new { error = message, subjectName });
        }
    }
}