using BusinessObject.Entities;
using DataAccessLayer;
using DataAccessLayer.Repositories.Interfaces;
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
        private readonly IFileUploadService _fileUploadService;

        public IndexingService(
            AppDbContext context,
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            ITextExtractionService textExtractionService,
            IChunkingService chunkingService,
            IEmbeddingService embeddingService,
            IFileUploadService fileUploadService)
        {
            _context = context;
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _textExtractionService = textExtractionService;
            _chunkingService = chunkingService;
            _embeddingService = embeddingService;
            _fileUploadService = fileUploadService;
        }

        public async Task<(bool success, string? errorMessage)> IndexDocumentAsync(Document document)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    document.IndexStatus = "Processing";
                    await _documentRepository.UpdateAsync(document);
                    await _documentRepository.SaveChangesAsync();

                    var (extractSuccess, extractedText, extractError) = await _textExtractionService.ExtractTextAsync(document.FilePath);
                    if (!extractSuccess)
                    {
                        await transaction.RollbackAsync();
                        document.IndexStatus = "Failed";
                        document.ErrorMessage = $"Text extraction failed: {extractError}";
                        await _documentRepository.UpdateAsync(document);
                        await _documentRepository.SaveChangesAsync();
                        _fileUploadService.DeleteFile(document.FilePath);
                        return (false, extractError);
                    }

                    var chunks = _chunkingService.ChunkText(extractedText ?? "");
                    if (chunks.Count == 0)
                    {
                        await transaction.RollbackAsync();
                        document.IndexStatus = "Failed";
                        document.ErrorMessage = "No chunks generated from document";
                        await _documentRepository.UpdateAsync(document);
                        await _documentRepository.SaveChangesAsync();
                        _fileUploadService.DeleteFile(document.FilePath);
                        return (false, "No chunks generated");
                    }

                    int chunkOrder = 0;
                    var documentChunks = new List<DocumentChunk>();
                    foreach (var chunk in chunks)
                    {
                        var (embedSuccess, embedding, embedError) = await _embeddingService.GetEmbeddingAsync(chunk);
                        if (!embedSuccess)
                        {
                            await transaction.RollbackAsync();
                            document.IndexStatus = "Failed";
                            document.ErrorMessage = $"Embedding failed: {embedError}";
                            await _documentRepository.UpdateAsync(document);
                            await _documentRepository.SaveChangesAsync();
                            _fileUploadService.DeleteFile(document.FilePath);
                            return (false, embedError);
                        }

                        var vectorData = new Vector((embedding ?? new List<float>()).ToArray());

                        documentChunks.Add(new DocumentChunk
                        {
                            DocumentId = document.Id,
                            Content = chunk,
                            Embedding = vectorData,
                            ChunkOrder = chunkOrder++
                        });
                    }

                    await _documentChunkRepository.AddRangeAsync(documentChunks);

                    document.IndexStatus = "Completed";
                    document.ErrorMessage = null;
                    await _documentRepository.UpdateAsync(document);
                    await _documentRepository.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    document.IndexStatus = "Failed";
                    document.ErrorMessage = $"Indexing error: {ex.Message}";
                    await _documentRepository.UpdateAsync(document);
                    await _documentRepository.SaveChangesAsync();
                    _fileUploadService.DeleteFile(document.FilePath);
                    return (false, ex.Message);
                }
            }
        }
    }
}