using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class DocumentChunk
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; } // Khóa ngoại liên kết ngược lại file gốc

        [Required(ErrorMessage = "Nội dung đoạn trích không được để trống.")]
        public string Content { get; set; } // Đoạn chữ thô (Text) sau khi băm nhỏ

        [Required]
        public string VectorJson { get; set; } // Mảng float[] được ép kiểu sang chuỗi JSON để lưu vào SQL Server

        public int ChunkOrder { get; set; } // Số thứ tự của đoạn chunk (Đoạn 1, Đoạn 2, Đoạn 3...)
    }
}