using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using ServiceLayer.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;

        public SubjectService(ISubjectRepository subjectRepository)
        {
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<Subject>> GetSubjects()
        {
            return await _subjectRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Subject>> GetSubjectsByTeacherId(System.Guid teacherAccountId)
        {
            return await _subjectRepository.GetByTeacherIdAsync(teacherAccountId);
        }

        public async Task<Subject> GetSubjectById(int id)
        {
            return await _subjectRepository.GetByIdAsync(id);
        }

        public async Task AddSubject(Subject subject)
        {
            await _subjectRepository.AddAsync(subject);
        }

        public async Task UpdateSubject(Subject subject)
        {
            await _subjectRepository.UpdateAsync(subject);
        }

        public async Task DeleteSubject(int id)
        {
            await _subjectRepository.DeleteAsync(id);
        }
    }
}
