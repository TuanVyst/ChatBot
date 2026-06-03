using BusinessObject.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetSubjects();
        Task<Subject> GetSubjectById(int id);
        Task AddSubject(Subject subject);
        Task UpdateSubject(Subject subject);
        Task DeleteSubject(int id);
    }
}
