using System.Collections.Generic;
using BusinessObject.Entities;

namespace ChatBot.Models
{
    public class DashboardViewModel
    {
        // Subjects now contains Subject entities so the UI can use Id and Name
        public IReadOnlyList<Subject> Subjects { get; init; } = new List<Subject>();
        public List<Document> Documents { get; init; } = new();
        // SelectedSubjectId stores the selected subject's Id (string GUID)
        public string? SelectedSubjectId { get; init; }
        public string ChapterName { get; init; } = "Default";
        public string? Message { get; init; }
        public string? Error { get; init; }
        public int PendingCount { get; init; }
    }
}
