using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên file không được để trống.")]
        [StringLength(255, ErrorMessage = "Tên file không được quá 255 ký tự.")]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; } // Đường dẫn vật lý lưu file trên thư viện Server để đọc

        public long FileSize { get; set; } // Dung lượng file (Bytes) để hiển thị lên UI

        [DataType(DataType.DateTime)]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Required]
        public Guid SubjectId { get; set; } // ID của môn học (Đề bài yêu cầu: Quản lý theo môn học/chương)

        public Guid ChapterId { get; set; } // Liên kết đến Chapter

        [ForeignKey("ChapterId")]
        public virtual Chapter? Chapter { get; set; }

        [Required]
        [StringLength(50)]
        public string IndexStatus { get; set; } = "Pending"; // Trạng thái indexing: Pending, Completed, Failed

        public string? ErrorMessage { get; set; } // Lưu thông báo lỗi nếu indexing thất bại

        // Quan hệ 1 - N: Một file tài liệu sau khi băm sẽ sinh ra nhiều đoạn văn bản nhỏ (Chunks)
        public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }
    }
}