using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObject.Entities;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IDocumentChunkRepository
    {
        Task AddRangeAsync(System.Collections.Generic.IEnumerable<DocumentChunk> chunks);
        Task DeleteByDocumentIdAsync(int documentId);
        Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(int documentId);
        Task SaveChangesAsync();
    }
}
