using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ChatBot.Models;
using ServiceLayer.Interfaces;
using BusinessObject.Entities;

namespace ChatBot.Controllers
{
    [Authorize]
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

        // View students in a subject (for lecture)
        [HttpGet]
        public async Task<IActionResult> StudentsInSubject(string subjectId)
        {
            if (string.IsNullOrEmpty(subjectId) || !Guid.TryParse(subjectId, out var sid))
                return BadRequest("Invalid subject id");

            // Load student-subject entries and include user info
            var students = await _subjectService.GetStudentsBySubjectIdAsync(sid);
            return PartialView("_StudentsList", students);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudentFromSubject(Guid accountId, Guid subjectId)
        {
            var (success, message) = await _subjectService.RemoveStudentFromSubjectAsync(accountId, subjectId);
            return Json(new { success, message });
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

       
        public async Task<IActionResult> Index(string? subjectName = null, string? chapterId = null, string? message = null, string? error = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var subjects = await _subjectService.GetSubjectsByTeacherId(userId);
            var subjectList = subjects.ToList();

            // We expect the query param to be a subject id (string GUID)
            string? selectedSubjectId = subjectName;
            if (!string.IsNullOrEmpty(selectedSubjectId) && !subjectList.Any(s => s.Id.ToString() == selectedSubjectId))
            {
                selectedSubjectId = subjectList.FirstOrDefault()?.Id.ToString();
            }

            // Load chapters for the selected subject (if any)
            var chapters = new List<BusinessObject.Entities.Chapter>();
            if (!string.IsNullOrEmpty(selectedSubjectId) && Guid.TryParse(selectedSubjectId, out var subjGuid))
            {
                var chs = await _chapterService.GetChaptersBySubjectIdAsync(subjGuid);
                chapters = chs.ToList();
            }

            // Selected chapter id comes from query as string GUID
            string? selectedChapterId = chapterId;
            if (!string.IsNullOrEmpty(selectedChapterId) && !chapters.Any(c => c.Id.ToString() == selectedChapterId))
            {
                // If the selected chapter doesn't belong to the selected subject, clear selection
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
                    var docs = await _documentService.GetDocumentsAsync(selectedSubjectId, selectedChapterId);
                    documents = docs.ToList();
                }
            }
            var pendingCount = documents.Count(d => string.Equals(d.IndexStatus, "Pending", StringComparison.OrdinalIgnoreCase));

            var model = new DashboardViewModel
            {
                Subjects = subjectList,
                Documents = documents.ToList(),
                SelectedSubjectId = selectedSubjectId,
                Chapters = chapters,
                SelectedChapterId = selectedChapterId,
                PendingCount = pendingCount,
                Message = message,
                Error = error,
            };
            return View(model);
        }

        // Return only the documents list portion as a partial view for AJAX updates
        [HttpGet]
        public async Task<IActionResult> DocumentsPartial(string? subjectName = null, string? chapterId = null)
        {
            var user = HttpContext.User;
            // Load chapters for the selected subject (if any) so selection validation can be applied
            var chapters = new List<Chapter>();
            if (!string.IsNullOrEmpty(subjectName) && Guid.TryParse(subjectName, out var subjGuid))
            {
                var chs = await _chapterService.GetChaptersBySubjectIdAsync(subjGuid);
                chapters = chs.ToList();
            }

            // Validate chapter belongs to the selected subject
            string? selectedChapterId = chapterId;
            if (!string.IsNullOrEmpty(selectedChapterId) && !chapters.Any(c => c.Id.ToString() == selectedChapterId))
            {
                selectedChapterId = null;
            }

            if (string.IsNullOrEmpty(subjectName) && chapters.Any())
            {
                // Do not force chapter selection if no subject is selected
            }

            var subjectList = await _subjectService.GetSubjectsByCurrentLecturer(user);

            var documents = new List<Document>();
            if (subjectList.Any())
            {
                if (string.IsNullOrEmpty(subjectName))
                {
                    var allDocs = await _documentService.GetDocumentsAsync(null, chapterId);
                    var ownedSubjectIds = subjectList.Select(s => s.Id).ToList();
                    documents = allDocs.Where(d => ownedSubjectIds.Contains(d.SubjectId)).ToList();
                }
                else
                {
                    var docs = await _documentService.GetDocumentsAsync(subjectName, chapterId);
                    documents = docs.ToList();
                }
            }

            var model = new DashboardViewModel
            {
                Documents = documents.ToList(),
                SelectedSubjectId = subjectName,
                SelectedChapterId = selectedChapterId,
                Chapters = chapters,
            };

            return PartialView("_DocumentsTable", model);
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
        public async Task<IActionResult> CreateChapter(Guid subjectId, string name, string? description)
        {
            var (success, msg, _) = await _chapterService.CreateChapterAsync(subjectId, name, description);
            if (success)
            {
                TempData["ChapterSuccess"] = msg;
            }
            else
            {
                TempData["ChapterError"] = msg;
            }
            return RedirectToAction(nameof(Index), new { subjectName = subjectId.ToString() });
        }

        // GET: show form for lecture to add a student to a subject
        [Authorize(Roles = "Lecture")]
        public async Task<IActionResult> AddStudentToSubject(string id)
        {
            var subject = await _subjectService.GetSubjectById(id);
            if (subject == null) return NotFound();
            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecture")]
        public async Task<IActionResult> AddStudentToSubject(string subjectId, string email)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subjectId))
            {
                TempData["StudentError"] = "Vui lòng nhập đầy đủ email và môn học.";
                return RedirectToAction(nameof(Index), new { subjectName = subjectId });
            }

            if (!Guid.TryParse(subjectId, out var subjGuid))
            {
                TempData["StudentError"] = "Môn học không hợp lệ.";
                return RedirectToAction(nameof(Index), new { subjectName = subjectId });
            }

            var (success, message) = await _subjectService.AddStudentToSubjectAsync(email.Trim(), subjGuid);

            if (success)
            {
                TempData["StudentSuccess"] = message;
            }
            else
            {
                TempData["StudentError"] = message;
            }

            return RedirectToAction(nameof(Index), new { subjectName = subjectId });
        }

      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string subjectName, string chapterId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return RedirectToAction("Login", "Auth");
            var subjects = await _subjectService.GetSubjectsByTeacherId(userId);
            // The UI sends the selected subject's Id as the subjectName form value.
            // Treat subjectName as subjectId here and validate by Id.
            var subjectId = subjectName;
            if (string.IsNullOrEmpty(subjectId) || !subjects.Any(s => s.Id.ToString() == subjectId))
            {
                TempData["UploadError"] = "Unauthorized subject";
                return RedirectToAction(nameof(Index));
            }

            var (success, message, _) = await _documentService.UploadDocumentAsync(file, subjectId, chapterId);

            if (success)
            {
                TempData["UploadSuccess"] = message;
            }
            else
            {
                TempData["UploadError"] = message;
            }

            return RedirectToAction(nameof(Index), new { subjectName = subjectId });
        }

      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reindex(int id, string? subjectName = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return RedirectToAction("Login", "Auth");
            
            if (!string.IsNullOrEmpty(subjectName))
            {
                var subjects = await _subjectService.GetSubjectsByTeacherId(userId);
                // subjectName in UI is actually the subject Id (string GUID). Validate by Id.
                if (!subjects.Any(s => s.Id.ToString() == subjectName))
                {
                    TempData["ListError"] = "Unauthorized subject";
                    return RedirectToAction(nameof(Index));
                }
            }

            var (success, message) = await _documentService.ReindexDocumentAsync(id);

            if (success)
            {
                TempData["ListSuccess"] = message;
            }
            else
            {
                TempData["ListError"] = message;
            }

            return RedirectToAction(nameof(Index), new { subjectName });
        }
    }
}