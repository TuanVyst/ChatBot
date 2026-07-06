using BusinessObject.Entities;
using DataAccessLayer;
using Pgvector;
using ServiceLayer.Interfaces;
using Pgvector.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ServiceLayer.Implements
{
    public class RetrievalService : IRetrievalService
    {
        private readonly AppDbContext _context; // đổi thành tên DbContext thực tế

        public RetrievalService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, List<DocumentChunk>? chunks, string? errorMessage)> SearchAsync(
            List<float> queryEmbedding, int topK = 5, Guid? subjectId = null, Guid? chapterId = null, int? documentId = null)
        {
            try
            {
                if (queryEmbedding == null || queryEmbedding.Count == 0)
                    return (false, null, "Query embedding is empty");

                var vector = new Vector(queryEmbedding.ToArray());

                var query = _context.DocumentChunks
                    .Include(c => c.Document)
                    .AsQueryable();

                if (documentId.HasValue)
                    query = query.Where(c => c.DocumentId == documentId.Value);

                if (subjectId.HasValue)
                    query = query.Where(c => c.Document!.SubjectId == subjectId.Value);

                if (chapterId.HasValue)
                    query = query.Where(c => c.Document!.ChapterId == chapterId.Value);

                var results = await query
                    .OrderBy(c => c.Embedding.CosineDistance(vector))
                    .Take(topK)
                    .ToListAsync();

                return (true, results, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Retrieval failed: {ex.Message}");
            }
        }
    }
}
