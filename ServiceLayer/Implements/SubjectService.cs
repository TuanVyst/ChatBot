using BusinessObject.Entities;
using DataAccessLayer;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;
        private readonly AppDbContext _context;

        public SubjectService(ISubjectRepository subjectRepository, AppDbContext context)
        {
            _subjectRepository = subjectRepository;
            _context = context;
        }

        public async Task<IEnumerable<Subject>> GetSubjects()
        {
            return await _subjectRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Subject>> GetSubjectsByTeacherId(System.Guid teacherAccountId)
        {
            return await _subjectRepository.GetByTeacherIdAsync(teacherAccountId);
        }

        public async Task<Subject> GetSubjectById(string id)
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

        public async Task DeleteSubject(string id)
        {
            await _subjectRepository.DeleteAsync(id);
        }

        public async Task<(bool Success, string Message)> AddStudentToSubjectAsync(string emailOrUsername, System.Guid subjectId)
        {
            var student = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == emailOrUsername || 
                    _context.UserInformations.Any(u => u.Account_id == a.Account_id && u.Email == emailOrUsername));

            if (student == null)
                return (false, "Không tìm thấy sinh viên với email/username này.");

            if (student.Role != BusinessObject.Enums.RoleEnum.User)
                return (false, "Tài khoản không phải là sinh viên.");

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId);
            if (subject == null)
                return (false, "Không tìm thấy môn học.");

            var existing = await _context.StudentSubjects
                .FirstOrDefaultAsync(ss => ss.AccountId == student.Account_id && ss.SubjectId == subjectId);

            if (existing != null)
                return (false, "Sinh viên đã có trong môn học này.");

            var studentSubject = new StudentSubject
            {
                AccountId = student.Account_id,
                SubjectId = subjectId
            };

            await _context.StudentSubjects.AddAsync(studentSubject);
            await _context.SaveChangesAsync();

            return (true, "Thêm sinh viên vào môn học thành công.");
        }
    }
}
