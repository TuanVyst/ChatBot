using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Entities
{
    public class ChatHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        public Guid? SubjectId { get; set; }

        public Guid? ChapterId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; }

        public int PromptTokens { get; set; }

        public int CompletionTokens { get; set; }

        public int TotalTokens { get; set; }

        public string ModelName { get; set; } = string.Empty;

        public virtual ICollection<ChatHistorySource> Sources { get; set; }
            = new List<ChatHistorySource>();
    }
}