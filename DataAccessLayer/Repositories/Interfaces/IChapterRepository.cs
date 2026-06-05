using BusinessObject.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IChapterRepository
    {
        Task<IEnumerable<Chapter>> GetChaptersBySubjectIdAsync(Guid subjectId);
        Task<Chapter?> GetByIdAsync(int id);
        Task AddAsync(Chapter chapter);
        Task SaveChangesAsync();
    }
}
