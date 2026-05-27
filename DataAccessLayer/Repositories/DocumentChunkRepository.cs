using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class DocumentChunkRepository : IDocumentChunkRepository
    {
        private readonly AppDbContext _context;

        public DocumentChunkRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<DocumentChunk> chunks)
        {
            await _context.DocumentChunks.AddRangeAsync(chunks);
        }

        public async Task DeleteByDocumentIdAsync(int documentId)
        {
            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ToListAsync();

            _context.DocumentChunks.RemoveRange(chunks);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
