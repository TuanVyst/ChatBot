using BusinessObject.Entities;

namespace ChatBot.Models
{
    public class StudentDashboardViewModel
    {
        public IEnumerable<Subject> Subjects { get; set; } = new List<Subject>();
    }
}