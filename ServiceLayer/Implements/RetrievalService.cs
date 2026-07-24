using BusinessObject.Entities;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using ServiceLayer.Interfaces;

namespace ServiceLayer.Implements
{
    public class RetrievalService : IRetrievalService
    {
        private readonly AppDbContext _context;

        public RetrievalService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(
            bool success,
            List<DocumentChunk>? chunks,
            string? errorMessage)>
        SearchAsync(
            List<float> queryEmbedding,
            int topK = 5,
            Guid? subjectId = null,
            Guid? chapterId = null,
            int? documentId = null,
            double? maxDistance = null)
        {
            try
            {
                if (queryEmbedding == null ||
                    queryEmbedding.Count == 0)
                {
                    return (
                        false,
                        null,
                        "Query embedding is empty.");
                }

                if (queryEmbedding.Count != 3072)
                {
                    return (
                        false,
                        null,
                        $"Query embedding dimension is " +
                        $"{queryEmbedding.Count}, expected 3072.");
                }

                if (topK <= 0)
                {
                    topK = 5;
                }

                var vector =
                    new Vector(queryEmbedding.ToArray());

                var query = _context.DocumentChunks
                    .AsNoTracking()
                    .Include(c => c.Document)
                    .AsQueryable();

                if (documentId.HasValue)
                {
                    query = query.Where(c =>
                        c.DocumentId == documentId.Value);
                }

                if (subjectId.HasValue)
                {
                    query = query.Where(c =>
                        c.Document != null &&
                        c.Document.SubjectId == subjectId.Value);
                }

                if (chapterId.HasValue)
                {
                    query = query.Where(c =>
                        c.Document != null &&
                        c.Document.ChapterId == chapterId.Value);
                }

                if (maxDistance.HasValue)
                {
                    query = query.Where(c =>
                        c.Embedding.CosineDistance(vector) <= maxDistance.Value);
                }

                var results = await query
                    .OrderBy(c =>
                        c.Embedding.CosineDistance(vector))
                    .Take(topK)
                    .ToListAsync();

                if (results.Count == 0)
                {
                    return (
                        false,
                        results,
                        "Không tìm thấy chunk phù hợp với bộ lọc.");
                }

                return (true, results, null);
            }
            catch (Exception ex)
            {
                return (
                    false,
                    null,
                    $"Retrieval failed: {ex.Message}");
            }
        }
    }
}