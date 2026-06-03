using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Implements
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly AppDbContext _context;

        public SubjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Subject>> GetAllAsync()
        {
            return await _context.Subjects.Include(s => s.University).Include(s => s.Teacher).ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetByTeacherIdAsync(System.Guid teacherAccountId)
        {
            return await _context.Subjects.Include(s => s.University).Where(s => s.LectureAccountId == teacherAccountId).ToListAsync();
        }

        public async Task<Subject> GetByIdAsync(string id)
        {
            return await _context.Subjects.Include(s => s.University).Include(s => s.Teacher).FirstOrDefaultAsync(s => s.Id.ToString() == id);
        }

        public async Task AddAsync(Subject subject)
        {
            await _context.Subjects.AddAsync(subject);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Subject subject)
        {
            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id.ToString() == id);
            if (subject != null)
            {
                _context.Subjects.Remove(subject);
                await _context.SaveChangesAsync();
            }
        }
    }
}
