using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; }

        [Required]
        public int UniversityId { get; set; }
        [ForeignKey("UniversityId")]
        public virtual University University { get; set; }

        public Guid? TeacherAccountId { get; set; }
        [ForeignKey("TeacherAccountId")]
        public virtual Account? Teacher { get; set; }
    }
}
