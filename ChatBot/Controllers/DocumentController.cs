using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(
            IFormFile file,
            [FromForm] string subjectName,
            [FromForm] string chapterName = "Default")
        {
            try
            {
                var result = await _documentService.UploadDocumentAsync(
                    file,
                    subjectName,
                    chapterName);

                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(new
                {
                    documentId = result.DocumentId,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] string? subjectName = null)
        {
            try
            {
                var documents = await _documentService.GetDocumentsAsync(subjectName);
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
                var success = await _documentService.ReindexDocumentAsync(id);

                if (!success)
                    return NotFound("Document not found or reindexing failed");

                return Ok("Document reindexed successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}