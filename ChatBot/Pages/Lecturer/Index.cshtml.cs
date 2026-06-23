using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessObject.Entities;
using ChatBot.Hubs;

using DataAccessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ServiceLayer.Interfaces;

namespace ChatBot.Pages.Lecturer;

[Authorize(Roles = "Lecture")]
public partial class IndexModel : PageModel
{
    private readonly IDocumentService _documentService;
    private readonly ISubjectService _subjectService;
    private readonly IChapterService _chapterService;
    private readonly IMemoryCache _cache;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly AppDbContext _context;

    public DashboardData Dashboard { get; set; } = new();

    public class DashboardData
    {
        public int TotalSubjects { get; set; }
        public int TotalStudents { get; set; }
        public int TotalDocuments { get; set; }
        public int PendingCount { get; set; }
        public string FullName { get; set; } = string.Empty;
        public IReadOnlyList<BusinessObject.Entities.Subject> Subjects { get; set; } = new List<BusinessObject.Entities.Subject>();
        public IReadOnlyList<BusinessObject.Entities.Chapter> Chapters { get; set; } = new List<BusinessObject.Entities.Chapter>();
        public IReadOnlyList<BusinessObject.Entities.Document> Documents { get; set; } = new List<BusinessObject.Entities.Document>();
        public string? SelectedSubjectId { get; set; }
        public string? SelectedChapterId { get; set; }
    }

    public IndexModel(
        IDocumentService documentService,
        ISubjectService subjectService,
        IChapterService chapterService,
        IMemoryCache cache,
        IHubContext<NotificationHub> hubContext,
        AppDbContext context)
    {
        _documentService = documentService;
        _subjectService = subjectService;
        _chapterService = chapterService;
        _cache = cache;
        _hubContext = hubContext;
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync(string? subjectName = null, string? chapterId = null)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Auth/Login");

        var subjects = await _subjectService.GetSubjectsByTeacherId(userId);
        var subjectList = subjects.ToList();

        string? selectedSubjectId = subjectName;
        if (!string.IsNullOrEmpty(selectedSubjectId) &&
            !subjectList.Any(s => s.Id.ToString() == selectedSubjectId))
        {
            selectedSubjectId = subjectList.FirstOrDefault()?.Id.ToString();
        }

        var chapters = new List<Chapter>();
        if (!string.IsNullOrEmpty(selectedSubjectId) &&
            Guid.TryParse(selectedSubjectId, out var subjGuid))
        {
            chapters = (await _chapterService.GetChaptersBySubjectIdAsync(subjGuid)).ToList();
        }

        string? selectedChapterId = chapterId;
        if (!string.IsNullOrEmpty(selectedChapterId) &&
            !chapters.Any(c => c.Id.ToString() == selectedChapterId))
        {
            selectedChapterId = null;
        }

        var documents = new List<Document>();

        if (subjectList.Any())
        {
            if (string.IsNullOrEmpty(selectedSubjectId))
            {
                var allDocs = await _documentService.GetDocumentsAsync(null, selectedChapterId);
                var ownedSubjectIds = subjectList.Select(s => s.Id).ToList();
                documents = allDocs.Where(d => ownedSubjectIds.Contains(d.SubjectId)).ToList();
            }
            else
            {
                documents = (await _documentService.GetDocumentsAsync(selectedSubjectId, selectedChapterId)).ToList();
            }
        }

        var pendingCount = documents.Count(d =>
            string.Equals(d.IndexStatus, "Pending", StringComparison.OrdinalIgnoreCase));

        var totalStudents = 0;
        foreach (var subj in subjectList)
        {
            var students = await _subjectService.GetStudentsBySubjectIdAsync(subj.Id);
            totalStudents += students.Count();
        }

        Dashboard = new DashboardData
        {
            Subjects = subjectList,
            Documents = documents,
            SelectedSubjectId = selectedSubjectId,
            Chapters = chapters,
            SelectedChapterId = selectedChapterId,
            PendingCount = pendingCount,
            TotalStudents = totalStudents,
            FullName = User.FindFirstValue(ClaimTypes.Name) ?? "Lecturer"
        };

        return Page();
    }
}
