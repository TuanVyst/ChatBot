using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class Chapter
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public Guid SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
