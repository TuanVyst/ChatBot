using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using ServiceLayer.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class UniversityService : IUniversityService
    {
        private readonly IUniversityRepository _universityRepository;

        public UniversityService(IUniversityRepository universityRepository)
        {
            _universityRepository = universityRepository;
        }

        public async Task<IEnumerable<University>> GetUniversities()
        {
            return await _universityRepository.GetAllAsync();
        }

        public async Task<University> GetUniversityById(int id)
        {
            return await _universityRepository.GetByIdAsync(id);
        }

        public async Task AddUniversity(University university)
        {
            await _universityRepository.AddAsync(university);
        }

        public async Task UpdateUniversity(University university)
        {
            await _universityRepository.UpdateAsync(university);
        }

        public async Task DeleteUniversity(int id)
        {
            await _universityRepository.DeleteAsync(id);
        }
    }
}
