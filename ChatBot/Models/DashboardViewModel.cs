using System.Collections.Generic;
using BusinessObject.Entities;

namespace ChatBot.Models
{
    public class DashboardViewModel
    {
        public List<Subject> Subjects { get; init; } = new List<Subject>();
        public List<Document> Documents { get; init; } = new();
        public string? SelectedSubject { get; init; }
        public List<Chapter> Chapters { get; init; } = new List<Chapter>();
        public int? SelectedChapterId { get; init; }
        public string? Message { get; init; }
        public string? Error { get; init; }
        public int PendingCount { get; init; }
    }
}
