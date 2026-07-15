using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObject.Entities;
using Microsoft.AspNetCore.Http;

namespace ServiceLayer.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetSubjects();
        Task<IEnumerable<Subject>> GetSubjectsByTeacherId(System.Guid teacherAccountId);
        Task<IEnumerable<Subject>> GetSubjectsByCurrentLecturer(ClaimsPrincipal user);
        Task<Subject> GetSubjectById(string id);
        Task AddSubject(Subject subject);
        Task UpdateSubject(Subject subject);
        Task DeleteSubject(string id);
        Task<(bool Success, string Message)> AddStudentToSubjectAsync(string emailOrUsername, System.Guid subjectId);
        Task<IEnumerable<UserInformation>> GetStudentsBySubjectIdAsync(System.Guid subjectId);
        Task<(bool Success, string Message)> RemoveStudentFromSubjectAsync(System.Guid accountId, System.Guid subjectId);
        Task<(bool Success, string Message)> ImportStudentsFromExcelAsync(Guid subjectId,IFormFile file,Guid teacherId);
    }
}
