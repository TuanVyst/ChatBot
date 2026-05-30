using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BusinessObject.Entities;
using DataAccessLayer.Repositories;
using ServiceLayer.Services;
namespace ChatBot.Controllers
{
    public class DocumentController : Controller
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly FileUploadService _fileUploadService;
        private readonly IndexingService _indexingService;
        public DocumentController(
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            IFormFile file,
            [FromForm] string subjectName,
            [FromForm] string chapterName = "Default")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return RedirectToAction("Index", "Home", new { error = "Vui lòng chọn file tải lên." });
                }

                if (string.IsNullOrWhiteSpace(subjectName))
                {
                    return RedirectToAction("Index", "Home", new { error = "Tên môn học không được để trống." });
                }

                // ... code lưu file giữ nguyên ...
                using (var stream = file.OpenReadStream())
                {
                    var (uploadSuccess, filePath, uploadError) = await _fileUploadService.UploadFileAsync(stream, file.FileName);
                    if (!uploadSuccess)
                    {
                        return RedirectToAction("Index", "Home", new { error = $"Lỗi lưu file: {uploadError}" });
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
                        UploadDate = DateTime.UtcNow
                    };

                    await _documentRepository.AddAsync(document);
                    await _documentRepository.SaveChangesAsync();

                    // LƯU Ý: Dòng này có thể mất rất nhiều thời gian với file lớn
                    var (indexSuccess, indexError) = await _indexingService.IndexDocumentAsync(document);

                    if (!indexSuccess)
                    {
                        return RedirectToAction("Index", "Home", new { error = $"File đã tải lên nhưng lỗi khi xử lý AI: {indexError}" });
                    }

                    return RedirectToAction("Index", "Home", new { message = "Tải lên và xử lý dữ liệu AI thành công!" });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home", new { error = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] string subjectName = null)
        {
            try
            {
                var documents = await _documentRepository.GetCompletedDocumentsAsync(subjectName);
                return View(documents);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home", new { error = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReindexDocument(int id)
        {
            try
            {
                var document = await _documentRepository.GetByIdWithChunksAsync(id);
                if (document == null)
                {
                    return RedirectToAction("Index", "Home", new { error = "Document not found" });
                }

                await _documentChunkRepository.DeleteByDocumentIdAsync(id);
                await _documentChunkRepository.SaveChangesAsync();

                var (indexSuccess, indexError) = await _indexingService.IndexDocumentAsync(document);
                if (!indexSuccess)
                {
                    return RedirectToAction("Index", "Home", new { error = $"Reindexing failed: {indexError}" });
                }
                return RedirectToAction("Index", "Home", new { message = "Document reindexed successfully" });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home", new { error = $"Error: {ex.Message}" });
            }
        }
    }
}
