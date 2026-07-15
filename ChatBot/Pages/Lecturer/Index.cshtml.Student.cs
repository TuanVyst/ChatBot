using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObject.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace ChatBot.Pages.Lecturer;

public partial class IndexModel
{
    public async Task<IActionResult> OnGetStudentsInSubjectAsync(string subjectId)
    {
        if (string.IsNullOrEmpty(subjectId) || !Guid.TryParse(subjectId, out var sid))
            return BadRequest("Invalid subject id");

        var students = await _subjectService.GetStudentsBySubjectIdAsync(sid);

        return new PartialViewResult
        {
            ViewName = "_StudentsList",
            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<IEnumerable<UserInformation>>(MetadataProvider, ModelState)
            {
                Model = students
            }
        };
    }

    public async Task<IActionResult> OnPostAddStudentToSubjectAsync(
        string subjectId,
        string email)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subjectId))
        {
            TempData["StudentError"] = "Vui lòng nhập đầy đủ email và môn học.";
            return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId });
        }

        if (!Guid.TryParse(subjectId, out var subjGuid))
        {
            TempData["StudentError"] = "Môn học không hợp lệ.";
            return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId });
        }

        var (success, message) =
            await _subjectService.AddStudentToSubjectAsync(email.Trim(), subjGuid);

        if (success)
        {
            TempData["StudentSuccess"] = message;

            var subject = await _subjectService.GetSubjectById(subjectId);
            var studentIdentifier = email.Trim();
            var student = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Username == studentIdentifier ||
                    _context.UserInformations.Any(u => u.Account_id == a.Account_id && u.Email == studentIdentifier));

            if (student != null)
            {
                var studentNotification = new StudentNotification
                {
                    AccountId = student.Account_id,
                    Type = "enrolled",
                    Message = $"Bạn đã được thêm vào môn học \"{subject?.Name ?? ""}\"",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.StudentNotifications.AddAsync(studentNotification);
                await _context.SaveChangesAsync();

                var notification = new
                {
                    id = studentNotification.Id,
                    type = "enrolled",
                    message = studentNotification.Message,
                    time = studentNotification.CreatedAt
                };

                await _hubContext.Clients.Group(student.Account_id.ToString())
                    .SendAsync("RefreshData", notification.message, notification);
            }
        }
        else
            TempData["StudentError"] = message;

        return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId });
    }

    public async Task<IActionResult> OnPostImportStudentsExcelAsync(
        Guid subjectId,
        IFormFile file)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var teacherId))
            return RedirectToPage("/Auth/Login");

        var oldStudents = (await _subjectService.GetStudentsBySubjectIdAsync(subjectId))
            .Select(s => s.Account_id).ToHashSet();

        var result =
            await _subjectService.ImportStudentsFromExcelAsync(
                subjectId,
                file,
                teacherId);

        if (result.Success)
        {
            TempData["StudentSuccess"] = result.Message;

            var subject = await _subjectService.GetSubjectById(subjectId.ToString());
            var allStudents = await _subjectService.GetStudentsBySubjectIdAsync(subjectId);
            var newStudents = allStudents.Where(s => !oldStudents.Contains(s.Account_id));
            foreach (var student in newStudents)
            {
                var studentNotification = new StudentNotification
                {
                    AccountId = student.Account_id,
                    Type = "enrolled",
                    Message = $"Bạn đã được thêm vào môn học \"{subject?.Name ?? ""}\"",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.StudentNotifications.AddAsync(studentNotification);
                await _context.SaveChangesAsync();

                var notification = new
                {
                    id = studentNotification.Id,
                    type = "enrolled",
                    message = studentNotification.Message,
                    time = studentNotification.CreatedAt
                };

                await _hubContext.Clients.Group(student.Account_id.ToString())
                    .SendAsync("RefreshData", notification.message, notification);
            }
        }
        else
            TempData["StudentError"] = result.Message;

        return RedirectToPage("/Lecturer/Index", new { subjectName = subjectId.ToString() });
    }

    public async Task<IActionResult> OnPostRemoveStudentAsync(
        Guid accountId,
        Guid subjectId)
    {
        var (success, message) =
            await _subjectService.RemoveStudentFromSubjectAsync(accountId, subjectId);

        if (success)
        {
            var subject = await _subjectService.GetSubjectById(subjectId.ToString());
            await _hubContext.Clients.Group(accountId.ToString())
                .SendAsync("RefreshData",
                    $"Bạn đã bị xóa khỏi môn học \"{subject?.Name ?? ""}\"");
        }

        return new JsonResult(new { success, message });
    }
}
