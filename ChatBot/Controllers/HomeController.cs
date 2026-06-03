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

        public HomeController(IDocumentService documentService)
        {
            _documentService = documentService;
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

       
        public async Task<IActionResult> Index(string? subjectName = null, string? message = null, string? error = null)
        {
            var documents = await _documentService.GetDocumentsAsync(subjectName);
            var pendingCount = documents.Count(d => string.Equals(d.IndexStatus, "Pending", StringComparison.OrdinalIgnoreCase));
            var model = new DashboardViewModel
            {
                Subjects = SubjectCatalog.Subjects,
                Documents = documents.ToList(),
                SelectedSubject = subjectName,
                PendingCount = pendingCount,
                Message = message,
                Error = error,
            };
            return View(model);
        }

      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string subjectName, string chapterName = "Default")
        {
            var (success, message, _) = await _documentService.UploadDocumentAsync(file, subjectName, chapterName);

            if (success)
            {
                return RedirectToAction(nameof(Index), new { message });
            }

            return RedirectToAction(nameof(Index), new { error = message });
        }

      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reindex(int id, string? subjectName = null)
        {
            var (success, message) = await _documentService.ReindexDocumentAsync(id);

            if (success)
            {
                return RedirectToAction(nameof(Index), new { message, subjectName });
            }

            return RedirectToAction(nameof(Index), new { error = message, subjectName });
        }
    }
}