using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class ChapterService : IChapterService
    {
        private readonly IChapterRepository _chapterRepository;

        public ChapterService(IChapterRepository chapterRepository)
        {
            _chapterRepository = chapterRepository;
        }

        public async Task<IEnumerable<Chapter>> GetChaptersBySubjectIdAsync(Guid subjectId)
        {
            return await _chapterRepository.GetChaptersBySubjectIdAsync(subjectId);
        }

        public async Task<(bool Success, string Message, Chapter? Chapter)> CreateChapterAsync(Guid subjectId, string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (false, "Tên chapter không được để trống", null);

            var chapter = new Chapter
            {
                Name = name,
                Description = description,
                SubjectId = subjectId
            };

            await _chapterRepository.AddAsync(chapter);
            await _chapterRepository.SaveChangesAsync();

            return (true, "Tạo chapter thành công", chapter);
        }
    }
}
