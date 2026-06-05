using BusinessObject.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IChapterService
    {
        Task<IEnumerable<Chapter>> GetChaptersBySubjectIdAsync(Guid subjectId);
        Task<(bool Success, string Message, Chapter? Chapter)> CreateChapterAsync(Guid subjectId, string name, string? description);
    }
}
