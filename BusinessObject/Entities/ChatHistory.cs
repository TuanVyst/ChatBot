using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class ChatHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Question { get; set; }

        [Required]
        public string Answer { get; set; }

        public Guid? SubjectId { get; set; }
        public Guid? ChapterId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UserId { get; set; } // hệ thống đăng nhập lưu Account ID (string/Guid)

        public virtual ICollection<ChatHistorySource> Sources { get; set; } = new List<ChatHistorySource>();
    }
}
