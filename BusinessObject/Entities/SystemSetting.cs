namespace BusinessObject.Entities;

public class SystemSetting
{
    public int Id { get; set; }

    public int ChunkSize { get; set; } = 512;

    public int ChunkOverlap { get; set; } = 50;

    public int TopK { get; set; } = 5;

    public string EmbeddingModel { get; set; } = "gemini-embedding-2";

    public string BackupEmbeddingModel { get; set; } = "gemini-embedding-2-preview";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}