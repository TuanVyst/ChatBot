using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Implements
{
    public class ChapterRepository : IChapterRepository
    {
        private readonly AppDbContext _context;

        public ChapterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Chapter>> GetChaptersBySubjectIdAsync(Guid subjectId)
        {
            return await _context.Chapters
                .Where(c => c.SubjectId == subjectId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Chapter?> GetByIdAsync(string id)
        {
            return await _context.Chapters.FirstOrDefaultAsync(c => c.Id.ToString() == id);
        }

        public async Task AddAsync(Chapter chapter)
        {
            await _context.Chapters.AddAsync(chapter);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
