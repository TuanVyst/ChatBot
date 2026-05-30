using System.Threading.Tasks;
using BusinessObject.Entities;

namespace DataAccessLayer.Repositories
{
    public interface IDocumentChunkRepository
    {
        Task AddRangeAsync(System.Collections.Generic.IEnumerable<DocumentChunk> chunks);
        Task DeleteByDocumentIdAsync(int documentId);
        Task SaveChangesAsync();
    }
}
