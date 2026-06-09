using BusinessObject.Entities;
using DataAccessLayer;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Interfaces;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using OfficeOpenXml;
using Microsoft.AspNetCore.Http;

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

        public async Task<IEnumerable<Subject>> GetSubjectsByCurrentLecturer(ClaimsPrincipal user)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userId, out var AccountId))
                return Enumerable.Empty<Subject>();

             return await GetSubjectsByTeacherId(AccountId);
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

            if (student.Role != BusinessObject.Enums.RoleEnum.Student)
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

        public async Task<IEnumerable<BusinessObject.Entities.UserInformation>> GetStudentsBySubjectIdAsync(System.Guid subjectId)
        {
            var q = from ss in _context.StudentSubjects
                    join u in _context.UserInformations on ss.AccountId equals u.Account_id
                    where ss.SubjectId == subjectId
                    select u;

            return await q.ToListAsync();
        }

        public async Task<(bool Success, string Message)> RemoveStudentFromSubjectAsync(System.Guid accountId, System.Guid subjectId)
        {
            var ss = await _context.StudentSubjects.FirstOrDefaultAsync(x => x.AccountId == accountId && x.SubjectId == subjectId);
            if (ss == null) return (false, "Không tìm thấy sinh viên trong môn học.");
            _context.StudentSubjects.Remove(ss);
            await _context.SaveChangesAsync();
            return (true, "Sinh viên đã bị xóa khỏi môn học.");
        }

        public async Task<(bool Success, string Message)> ImportStudentsFromExcelAsync(
    Guid subjectId,
    IFormFile file,
    Guid teacherId)
        {
            if (file == null || file.Length == 0)
                return (false, "Vui lòng chọn file Excel.");

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == subjectId && s.LectureAccountId == teacherId);

            if (subject == null)
                return (false, "Bạn không có quyền import sinh viên vào môn học này.");

            var extension = Path.GetExtension(file.FileName).ToLower();

            if (extension != ".xlsx" && extension != ".xls")
                return (false, "Chỉ hỗ trợ file Excel .xlsx hoặc .xls.");

            int addedCount = 0;
            int skippedCount = 0;
            int createdAccountCount = 0;

            ExcelPackage.License.SetNonCommercialOrganization("FPT University");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null || worksheet.Dimension == null)
                return (false, "File Excel không có dữ liệu.");

            int rowCount = worksheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var studentCode = worksheet.Cells[row, 1].Text.Trim();
                var fullName = worksheet.Cells[row, 2].Text.Trim();
                var email = worksheet.Cells[row, 3].Text.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    skippedCount++;
                    continue;
                }

                var userInfo = await _context.UserInformations
                    .Include(u => u.Account)
                    .FirstOrDefaultAsync(u => u.Email == email);

                Account studentAccount;

                if (userInfo == null)
                {
                    studentAccount = new Account
                    {
                        Account_id = Guid.NewGuid(),
                        Username = string.IsNullOrWhiteSpace(studentCode) ? email : studentCode,
                        Password = "123456",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        LastLogin = DateTime.UtcNow,
                        Role = BusinessObject.Enums.RoleEnum.Student
                    };

                    userInfo = new UserInformation
                    {
                        User_id = Guid.NewGuid(),
                        Account_id = studentAccount.Account_id,
                        Name = string.IsNullOrWhiteSpace(fullName) ? studentCode : fullName,
                        Email = email
                    };

                    await _context.Accounts.AddAsync(studentAccount);
                    await _context.UserInformations.AddAsync(userInfo);

                    createdAccountCount++;
                }
                else
                {
                    studentAccount = userInfo.Account;

                    if (studentAccount.Role != BusinessObject.Enums.RoleEnum.Student)
                    {
                        skippedCount++;
                        continue;
                    }
                }

                var existed = await _context.StudentSubjects
                    .AnyAsync(ss => ss.AccountId == studentAccount.Account_id &&
                                    ss.SubjectId == subjectId);

                if (existed)
                {
                    skippedCount++;
                    continue;
                }

                var studentSubject = new StudentSubject
                {
                    AccountId = studentAccount.Account_id,
                    SubjectId = subjectId,
                    EnrolledAt = DateTime.UtcNow
                };

                await _context.StudentSubjects.AddAsync(studentSubject);
                addedCount++;
            }

            await _context.SaveChangesAsync();

            return (true,
                $"Import thành công. Thêm vào lớp: {addedCount}, tạo tài khoản mới: {createdAccountCount}, bỏ qua: {skippedCount}.");
        }
    }
}
