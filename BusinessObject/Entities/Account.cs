using BusinessObject.Enums;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace BusinessObject.Entities
{
    public class Account
    {
        [Key]
        public Guid Account_id { get; set; } = Guid.NewGuid();

        [ForeignKey("Role")]
        public RoleEnum Role { get; set; } = RoleEnum.Student;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        public string Password { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public DateTime? LastLogin { get; set; }

        public virtual ICollection<Subject> Subjects { get; set; }
    }
}
