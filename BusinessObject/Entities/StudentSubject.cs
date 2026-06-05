using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class StudentSubject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Student { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    }
}
