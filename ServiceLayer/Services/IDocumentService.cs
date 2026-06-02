using Microsoft.AspNetCore.Http;
using BusinessObject.Entities;

namespace ServiceLayer.Services
{
    public interface IDocumentService
    {
        Task<(bool Success, string Message, int DocumentId)> UploadDocumentAsync(
            IFormFile file,
            string subjectName,
            string chapterName);

        Task<IEnumerable<Document>> GetDocumentsAsync(string subjectName);

        Task<(bool Success, string Message)> ReindexDocumentAsync(int id);
    }
}