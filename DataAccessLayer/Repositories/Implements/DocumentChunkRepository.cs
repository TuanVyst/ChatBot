using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories.Implements
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

        public async Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(int documentId)
        {
            return await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkOrder)
                .ToListAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
