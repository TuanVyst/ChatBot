using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class Subject
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        
        public string  Code { get; set; }

        [Required]
        public int UniversityId { get; set; }

        public Guid? LectureAccountId { get; set; }

        [ForeignKey("LectureAccountId")]
        public virtual Account? Teacher { get; set; }

        [ForeignKey("UniversityId")]
        public virtual University? University { get; set; }

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
