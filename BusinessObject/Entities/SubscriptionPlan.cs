using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Entities
{
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int Price { get; set; }

        [Required]
        public int DurationDays { get; set; }

        [Required]
        public int DailyQuestionLimit { get; set; } = 10;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
