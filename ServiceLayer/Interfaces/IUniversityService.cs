using BusinessObject.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IUniversityService
    {
        Task<IEnumerable<University>> GetUniversities();
        Task<University> GetUniversityById(int id);
        Task AddUniversity(University university);
        Task UpdateUniversity(University university);
        Task DeleteUniversity(int id);
    }
}
