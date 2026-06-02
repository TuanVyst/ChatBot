
using Microsoft.AspNetCore.Mvc;
using BusinessObject.Entities;
using DataAccessLayer.Repositories;
using ServiceLayer.Implements;
using ServiceLayer.Interfaces;
namespace ChatBot.Controllers
{
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
   
        private readonly FileUploadService _fileUploadService;
        private readonly IndexingService _indexingService;
        public DocumentController(
            IDocumentService documentService,
 
            FileUploadService fileUploadService,
            IndexingService indexingService)
        {
            _documentService = documentService;

            _fileUploadService = fileUploadService;
            _indexingService = indexingService;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<(bool Success, string Message, int DocumentId)> UploadDocument(
            IFormFile file,
            [FromForm] string subjectName,
            [FromForm] string chapterName = "Default")
        {
            return await _documentService.UploadDocumentAsync(file, subjectName, chapterName);

            
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] string subjectName = null)
        {
            try
            {
                var documents = await _documentService.GetDocumentsAsync(subjectName);
                return View(documents);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home", new { error = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<(bool Success, string Message)> ReindexDocument(int id)
        {
            try {  
                var document = await _documentService.ReindexDocumentAsync(id);
               

            return document;


            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }
    }
}
