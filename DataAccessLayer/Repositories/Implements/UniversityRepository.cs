using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Implements
{
    public class UniversityRepository : IUniversityRepository
    {
        private readonly AppDbContext _context;

        public UniversityRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<University>> GetAllAsync()
        {
            return await _context.Universities.ToListAsync();
        }

        public async Task<University> GetByIdAsync(int id)
        {
            return await _context.Universities.FindAsync(id);
        }

        public async Task AddAsync(University university)
        {
            await _context.Universities.AddAsync(university);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(University university)
        {
            _context.Universities.Update(university);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var university = await _context.Universities.FindAsync(id);
            if (university != null)
            {
                _context.Universities.Remove(university);
                await _context.SaveChangesAsync();
            }
        }
    }
}
