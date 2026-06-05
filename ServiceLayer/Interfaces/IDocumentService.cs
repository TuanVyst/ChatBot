using Microsoft.AspNetCore.Http;
using BusinessObject.Entities;

namespace ServiceLayer.Interfaces
{
    public interface IDocumentService
    {
        Task<(bool Success, string Message, int DocumentId)> UploadDocumentAsync(
            IFormFile file,
            string subjectId,
            string chapterName);

        Task<IEnumerable<Document>> GetDocumentsAsync(string subjectName, int? chapterId = null);

        Task<Document?> GetByIdAsync(int id);

        Task<(bool Success, string Message)> ReindexDocumentAsync(int id);
    }
}