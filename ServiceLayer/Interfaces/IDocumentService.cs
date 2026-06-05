using Microsoft.AspNetCore.Http;
using BusinessObject.Entities;

namespace ServiceLayer.Interfaces
{
    public interface IDocumentService
    {
        Task<(bool Success, string Message, int DocumentId)> UploadDocumentAsync(
            IFormFile file,
            string subjectId,
            string chapterId);
        Task<IEnumerable<Document>> GetDocumentsAsync(string subjectId, string? chapterId = null);

        Task<Document?> GetByIdAsync(int id);

        Task<(bool Success, string Message)> ReindexDocumentAsync(int id);
    }
}