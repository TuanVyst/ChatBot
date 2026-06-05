using BusinessObject.Entities;

namespace ChatBot.Models
{
    public class StudentChatViewModel
    {
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
        public IEnumerable<Document> Documents { get; set; } = new List<Document>();
        public string FullName { get; set; } = "Student";
        public Guid? SelectedSubjectId { get; set; }
        public int? SelectedDocumentId { get; set; }
    }
}
