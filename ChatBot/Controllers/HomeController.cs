using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.Entities;
using ServiceLayer.Services;
using ChatBot.Models;
using DataAccessLayer.Repositories.Interfaces;

namespace ChatBot.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly FileUploadService _fileUploadService;
        private readonly IndexingService _indexingService;

        public HomeController(
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            FileUploadService fileUploadService,
            IndexingService indexingService)
        {
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _fileUploadService = fileUploadService;
            _indexingService = indexingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? subjectName = null, string? message = null, string? error = null)
        {
            var documents = await _documentRepository.GetCompletedDocumentsAsync(subjectName);
            var pendingCount = documents.Count(d => string.Equals(d.IndexStatus, "Pending", StringComparison.OrdinalIgnoreCase));
            var model = new DashboardViewModel
            {
                Subjects = SubjectCatalog.Subjects,
                Documents = documents,
                SelectedSubject = subjectName,
                PendingCount = pendingCount,
                Message = message,
                Error = error,
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string subjectName, string chapterName = "Default")
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(Index), new { error = "Vui long chon file tai len." });
            }

            if (string.IsNullOrWhiteSpace(subjectName))
            {
                return RedirectToAction(nameof(Index), new { error = "Ten mon hoc khong duoc de trong." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var (uploadSuccess, filePath, uploadError) = await _fileUploadService.UploadFileAsync(stream, file.FileName);
                if (!uploadSuccess)
                {
                    return RedirectToAction(nameof(Index), new { error = $"Loi luu file: {uploadError}" });
                }

                var fileSize = _fileUploadService.GetFileSize(filePath);
                var document = new Document
                {
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileSize = fileSize,
                    SubjectName = subjectName,
                    ChapterName = chapterName,
                    IndexStatus = "Pending",
                    UploadDate = DateTime.UtcNow,
                };

                await _documentRepository.AddAsync(document);
                await _documentRepository.SaveChangesAsync();

                var (indexSuccess, indexError) = await _indexingService.IndexDocumentAsync(document);
                if (!indexSuccess)
                {
                    return RedirectToAction(nameof(Index), new { error = $"File da tai len nhung loi khi xu ly AI: {indexError}" });
                }

                return RedirectToAction(nameof(Index), new { message = "Tai len va xu ly du lieu AI thanh cong!" });
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index), new { error = $"Loi he thong: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reindex(int id, string? subjectName = null)
        {
            try
            {
                var document = await _documentRepository.GetByIdWithChunksAsync(id);
                if (document == null)
                {
                    return RedirectToAction(nameof(Index), new { error = "Document not found", subjectName });
                }

                await _documentChunkRepository.DeleteByDocumentIdAsync(id);
                await _documentChunkRepository.SaveChangesAsync();

                var (indexSuccess, indexError) = await _indexingService.IndexDocumentAsync(document);
                if (!indexSuccess)
                {
                    return RedirectToAction(nameof(Index), new { error = $"Reindexing failed: {indexError}", subjectName });
                }

                return RedirectToAction(nameof(Index), new { message = "Document reindexed successfully", subjectName });
            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index), new { error = $"Error: {ex.Message}", subjectName });
            }
        }
    }
}
