using BusinessObject.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetSubjects();
        Task<IEnumerable<Subject>> GetSubjectsByTeacherId(System.Guid teacherAccountId);
        Task<Subject> GetSubjectById(string id);
        Task AddSubject(Subject subject);
        Task UpdateSubject(Subject subject);
        Task DeleteSubject(string id);
    }
}
