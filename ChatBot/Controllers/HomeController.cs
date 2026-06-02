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
