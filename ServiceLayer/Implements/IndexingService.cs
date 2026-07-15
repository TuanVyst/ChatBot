using BusinessObject.Entities;
using DataAccessLayer;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Pgvector;
using ServiceLayer.Interfaces;

namespace ServiceLayer.Implements
{
    public class IndexingService : IIndexingService
    {
        private readonly AppDbContext _context;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly ITextExtractionService _textExtractionService;
        private readonly IChunkingService _chunkingService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IMemoryCache _cache;
        private readonly ISystemSettingService _settingService;

        public IndexingService(
            AppDbContext context,
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            ITextExtractionService textExtractionService,
            IChunkingService chunkingService,
            IEmbeddingService embeddingService,
            IFileUploadService fileUploadService,
            IMemoryCache cache,
            ISystemSettingService settingService)
        {
            _context = context;
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _textExtractionService = textExtractionService;
            _chunkingService = chunkingService;
            _embeddingService = embeddingService;
            _cache = cache;
            _settingService = settingService;
        }

        public async Task<(bool success, string? errorMessage)>
            IndexDocumentAsync(Document document)
        {
            var progressKey = $"doc_progress_{document.Id}";
            _cache.Set(progressKey, 0);

            try
            {
                // Lưu Processing riêng, không đặt trong transaction chính.
                document.IndexStatus = "Processing";
                document.ErrorMessage = null;

                await _documentRepository.UpdateAsync(document);
                await _documentRepository.SaveChangesAsync();

                // 1. Đọc nội dung file
                var (extractSuccess, extractedText, extractError) =
                    await _textExtractionService.ExtractTextAsync(
                        document.FilePath);

                if (!extractSuccess)
                {
                    await MarkAsFailedAsync(
                        document,
                        $"Text extraction failed: {extractError}");

                    return (false, extractError);
                }

                // 2. Lấy chunk size từ cấu hình Admin
                var setting = await _settingService.GetSettingAsync();

                var chunks = _chunkingService.ChunkText(
                    extractedText ?? string.Empty,
                    setting.ChunkSize,
                    setting.ChunkOverlap);

                if (chunks.Count == 0)
                {
                    const string error = "No chunks generated from document";

                    await MarkAsFailedAsync(document, error);

                    return (false, error);
                }

                var documentChunks = new List<DocumentChunk>();

                // 3. Tạo embedding cho từng chunk
                for (var index = 0; index < chunks.Count; index++)
                {
                    var chunk = chunks[index];

                    var (embedSuccess, embedding, embedError) =
                        await _embeddingService.GetEmbeddingAsync(chunk);

                    if (!embedSuccess || embedding == null)
                    {
                        await MarkAsFailedAsync(
                            document,
                            $"Embedding failed: {embedError}");

                        return (false, embedError);
                    }

                    // Database đang dùng vector(3072)
                    if (embedding.Count != 3072)
                    {
                        var dimensionError =
                            $"Embedding dimension is {embedding.Count}, expected 3072.";

                        await MarkAsFailedAsync(
                            document,
                            dimensionError);

                        return (false, dimensionError);
                    }

                    documentChunks.Add(new DocumentChunk
                    {
                        DocumentId = document.Id,
                        Content = chunk,
                        Embedding = new Vector(embedding.ToArray()),
                        ChunkOrder = index
                    });

                    var progress =
                        (int)(((index + 1) * 100.0) / chunks.Count);

                    _cache.Set(progressKey, progress);
                }

                // 4. Chỉ dùng transaction cho thao tác ghi chunks
                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    await _documentChunkRepository
                        .AddRangeAsync(documentChunks);

                    document.IndexStatus = "Completed";
                    document.ErrorMessage = null;

                    await _documentRepository.UpdateAsync(document);
                    await _documentRepository.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                _cache.Set(progressKey, 100);

                return (true, null);
            }
            catch (Exception ex)
            {
                await MarkAsFailedAsync(
                    document,
                    $"Indexing error: {ex.Message}");

                return (false, ex.Message);
            }
        }

        private async Task MarkAsFailedAsync(
            Document document,
            string errorMessage)
        {
            document.IndexStatus = "Failed";
            document.ErrorMessage = errorMessage;

            await _documentRepository.UpdateAsync(document);
            await _documentRepository.SaveChangesAsync();

            _cache.Remove($"doc_progress_{document.Id}");

            // Không xóa file gốc.
            // Sau khi sửa API key hoặc model vẫn có thể Re-index.
        }
    }
}