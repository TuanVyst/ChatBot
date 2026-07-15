using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessObject.Entities
{
    public class ChatHistorySource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatHistoryId { get; set; }
        [ForeignKey("ChatHistoryId")]
        public virtual ChatHistory? ChatHistory { get; set; }

        [Required]
        public int DocumentChunkId { get; set; }
        [ForeignKey("DocumentChunkId")]
        public virtual DocumentChunk? DocumentChunk { get; set; }
    }
}
