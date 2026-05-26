using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObject.Entities;
using DataAccessLayer;
using ServiceLayer.Services;
namespace ChatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FileUploadService _fileUploadService;
        private readonly IndexingService _indexingService;
        public DocumentController(
            AppDbContext context,
            FileUploadService fileUploadService,
            IndexingService indexingService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _indexingService = indexingService;
        }
        [HttpPost("upload")]
        // Xóa [FromQuery], đổi thành [FromForm]
        public async Task<IActionResult> UploadDocument(
     IFormFile file,
     [FromForm] string subjectName,
     [FromForm] string chapterName = "Default")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Vui lòng chọn file tải lên.");

                if (string.IsNullOrWhiteSpace(subjectName))
                    return BadRequest("Tên môn học không được để trống.");

                // ... code lưu file giữ nguyên ...
                using (var stream = file.OpenReadStream())
                {
                    var (uploadSuccess, filePath, uploadError) = await _fileUploadService.UploadFileAsync(stream, file.FileName);
                    if (!uploadSuccess)
                        return BadRequest($"Lỗi lưu file: {uploadError}");

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

                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();

                    // LƯU Ý: Dòng này có thể mất rất nhiều thời gian với file lớn
                    var (indexSuccess, indexError) = await _indexingService.IndexDocumentAsync(document);

                    if (!indexSuccess)
                        return BadRequest($"File đã tải lên nhưng lỗi khi xử lý AI: {indexError}");

                    return Ok(new
                    {
                        documentId = document.Id,
                        message = "Tải lên và xử lý dữ liệu AI thành công!"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] string subjectName = null)
        {
            try
            {
                IQueryable<Document> query = _context.Documents;
                if (!string.IsNullOrWhiteSpace(subjectName))
                    query = query.Where(d => d.SubjectName == subjectName);
                var documents = await query.Where(d => d.IndexStatus == "Completed").ToListAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPost("{id}/reindex")]
        public async Task<IActionResult> ReindexDocument(int id)
        {
            try
            {
                var document = await _context.Documents.Include(d => d.DocumentChunks).FirstOrDefaultAsync(d => d.Id == id);
                if (document == null)
                    return NotFound("Document not found");
                var chunks = _context.DocumentChunks.Where(c => c.DocumentId == id);
                _context.DocumentChunks.RemoveRange(chunks);
                await _context.SaveChangesAsync();
                var (indexSuccess, indexError) = await _indexingService.IndexDocumentAsync(document);
                if (!indexSuccess)
                    return BadRequest($"Reindexing failed: {indexError}");
                return Ok("Document reindexed successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
