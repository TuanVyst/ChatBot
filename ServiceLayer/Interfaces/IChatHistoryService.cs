using BusinessObject.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IChatHistoryService
    {
        Task<(bool success, string? errorMessage)> SaveAsync(
            string question, string answer, List<DocumentChunk> retrievedChunks,
            Guid? subjectId, Guid? chapterId, string? userId);

        Task<(bool success, List<ChatHistory>? history, string? errorMessage)> GetHistoryAsync(
            string? userId, Guid? subjectId = null, Guid? chapterId = null, int take = 20);
    }
}
