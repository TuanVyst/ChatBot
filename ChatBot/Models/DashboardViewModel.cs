using System.Collections.Generic;
using BusinessObject.Entities;

namespace ChatBot.Models
{
    public class DashboardViewModel
    {
        public IReadOnlyList<string> Subjects { get; init; } = new List<string>();
        public List<Document> Documents { get; init; } = new();
        public string? SelectedSubject { get; init; }
        public string ChapterName { get; init; } = "Default";
        public string? Message { get; init; }
        public string? Error { get; init; }
        public int PendingCount { get; init; }
    }
}
