using BusinessObject.Entities;

namespace ServiceLayer.Interfaces
{
    public interface IRagService
    {
        Task<(bool success, RagResult? result, string? errorMessage)> AskAsync(
            string question,
            Guid? subjectId = null,
            Guid? chapterId = null,
            int? documentId = null,
            string? userId = null);
    }
}