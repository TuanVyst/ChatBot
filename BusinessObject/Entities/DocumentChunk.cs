using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector; // Chắc chắn dùng cho PostgreSQL

namespace BusinessObject.Entities
{
    public class DocumentChunk
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [Required(ErrorMessage = "Nội dung đoạn trích không được để trống.")]
        public string Content { get; set; }

        [Required]
        [Column(TypeName = "vector(3072)")] // ÉP BUỘC EF CORE HIỂU ĐÂY LÀ VECTOR 3072 CHIỀU
        public Vector Embedding { get; set; }

        public int ChunkOrder { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }
    }
}