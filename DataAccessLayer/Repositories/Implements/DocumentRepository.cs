using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories.Implements
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
            return await _context.Documents
                .Include(d => d.Subject)
                .Include(d => d.DocumentChunks)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Document?> GetByIdWithChunksAsync(int id)
        {
            return await _context.Documents
                .Include(d => d.DocumentChunks)
                .Include(d => d.Subject)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Document>> GetCompletedDocumentsAsync(string? subjectId = null, string? chapterId = null)
        {
            IQueryable<Document> query = _context.Documents.Include(d => d.Subject).Include(d => d.Chapter);

            if (!string.IsNullOrWhiteSpace(subjectId) && Guid.TryParse(subjectId, out var parsedSubjectId))
            {
                query = query.Where(d => d.SubjectId == parsedSubjectId);
            }
            else if (!string.IsNullOrWhiteSpace(subjectId))
            {
                 // Handle subjectName case from old implementation
                 query = query.Where(d => d.Subject.Name == subjectId);
            }

            if (!string.IsNullOrWhiteSpace(chapterId))
            {
                // ChapterId is stored as a Guid, so compare by string form
                query = query.Where(d => d.ChapterId.ToString() == chapterId);
            }

            return await query.ToListAsync();
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
