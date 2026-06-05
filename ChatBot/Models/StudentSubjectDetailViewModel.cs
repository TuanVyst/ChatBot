using BusinessObject.Entities;

namespace ChatBot.Models
{
    public class StudentSubjectDetailViewModel
    {
        public Subject? Subject { get; set; }

        public IEnumerable<Document> Documents { get; set; } = new List<Document>();
    }
}