using BusinessObject.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface ISubjectRepository
    {
        Task<IEnumerable<Subject>> GetAllAsync();
        Task<Subject> GetByIdAsync(int id);
        Task AddAsync(Subject subject);
        Task UpdateAsync(Subject subject);
        Task DeleteAsync(int id);
    }
}
