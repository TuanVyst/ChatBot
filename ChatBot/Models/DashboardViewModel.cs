using System.Collections.Generic;
using BusinessObject.Entities;

namespace ChatBot.Models
{
    public class DashboardViewModel
    {
        public IReadOnlyList<Subject> Subjects { get; init; } = new List<Subject>();
        public List<Document> Documents { get; init; } = new();
        public string? SelectedSubjectId { get; init; }
        public IReadOnlyList<BusinessObject.Entities.Chapter> Chapters { get; init; } = new List<BusinessObject.Entities.Chapter>();
        public string? SelectedChapterId { get; init; }
        public string ChapterName { get; init; } = "Default";
        public string? Message { get; init; }
        public string? Error { get; init; }
        public int PendingCount { get; init; }
        public int TotalStudents { get; init; }
        public string FullName { get; init; } = "Lecturer";
    }
}
