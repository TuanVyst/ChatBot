using BusinessObject.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces
{
    public interface IUniversityRepository
    {
        Task<IEnumerable<University>> GetAllAsync();
        Task<University> GetByIdAsync(int id);
        Task AddAsync(University university);
        Task UpdateAsync(University university);
        Task DeleteAsync(int id);
    }
}
