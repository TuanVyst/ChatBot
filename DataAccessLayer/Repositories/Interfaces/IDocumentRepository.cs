using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObject.Entities;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IDocumentRepository
    {
        Task AddAsync(Document document);
        Task<Document?> GetByIdAsync(int id);
        Task<Document?> GetByIdWithChunksAsync(int id);
        Task<List<Document>> GetCompletedDocumentsAsync(string? subjectId = null, string? chapterId = null);
        Task UpdateAsync(Document document);
        Task DeleteAsync(Document document);
        Task SaveChangesAsync();
    }
}
