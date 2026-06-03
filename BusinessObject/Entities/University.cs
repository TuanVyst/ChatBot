using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Entities
{
    public class University
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        public virtual ICollection<Subject> Subjects { get; set; }
    }
}
