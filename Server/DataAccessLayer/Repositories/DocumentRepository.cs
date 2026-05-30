using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Document document)
        {
            await _context.Documents.AddAsync(document);
        }

        public async Task<Document?> GetByIdAsync(int id)
        {
            return await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Document?> GetByIdWithChunksAsync(int id)
        {
            return await _context.Documents
                .Include(d => d.DocumentChunks)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Document>> GetCompletedDocumentsAsync(string? subjectName = null)
        {
            IQueryable<Document> query = _context.Documents;

            if (!string.IsNullOrWhiteSpace(subjectName))
            {
                query = query.Where(d => d.SubjectName == subjectName);
            }

            return await query
                .Where(d => d.IndexStatus == "Completed")
                .ToListAsync();
        }

        public Task UpdateAsync(Document document)
        {
            _context.Documents.Update(document);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
