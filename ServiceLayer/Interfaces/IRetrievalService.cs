using BusinessObject.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IRetrievalService
    {
        Task<(bool success, List<DocumentChunk>? chunks, string? errorMessage)> SearchAsync(
            List<float> queryEmbedding, int topK = 5, Guid? subjectId = null, Guid? chapterId = null, int? documentId = null, double? maxDistance = null);
    }
}
