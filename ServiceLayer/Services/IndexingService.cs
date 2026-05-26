using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObject.Entities;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using Pgvector; // BẮT BUỘC THÊM DÒNG NÀY

namespace ServiceLayer.Services
{
    public class IndexingService
    {
        private readonly AppDbContext _context;
        private readonly TextExtractionService _textExtractionService;
        private readonly ChunkingService _chunkingService;
        private readonly EmbeddingService _embeddingService;
        private readonly FileUploadService _fileUploadService;

        public IndexingService(
            AppDbContext context,
            TextExtractionService textExtractionService,
            ChunkingService chunkingService,
            EmbeddingService embeddingService,
            FileUploadService fileUploadService)
        {
            _context = context;
            _textExtractionService = textExtractionService;
            _chunkingService = chunkingService;
            _embeddingService = embeddingService;
            _fileUploadService = fileUploadService;
        }

        public async Task<(bool success, string? errorMessage)> IndexDocumentAsync(Document document)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    document.IndexStatus = "Processing";
                    _context.Documents.Update(document);
                    await _context.SaveChangesAsync();

                    var (extractSuccess, extractedText, extractError) = await _textExtractionService.ExtractTextAsync(document.FilePath);
                    if (!extractSuccess)
                    {
                        await transaction.RollbackAsync();
                        document.IndexStatus = "Failed";
                        document.ErrorMessage = $"Text extraction failed: {extractError}";
                        _context.Documents.Update(document);
                        await _context.SaveChangesAsync();
                        _fileUploadService.DeleteFile(document.FilePath);
                        return (false, extractError);
                    }

                    var chunks = _chunkingService.ChunkText(extractedText ?? "");
                    if (chunks.Count == 0)
                    {
                        await transaction.RollbackAsync();
                        document.IndexStatus = "Failed";
                        document.ErrorMessage = "No chunks generated from document";
                        _context.Documents.Update(document);
                        await _context.SaveChangesAsync();
                        _fileUploadService.DeleteFile(document.FilePath);
                        return (false, "No chunks generated");
                    }

                    int chunkOrder = 0;
                    foreach (var chunk in chunks)
                    {
                        var (embedSuccess, embedding, embedError) = await _embeddingService.GetEmbeddingAsync(chunk);
                        if (!embedSuccess)
                        {
                            await transaction.RollbackAsync();
                            document.IndexStatus = "Failed";
                            document.ErrorMessage = $"Embedding failed: {embedError}";
                            _context.Documents.Update(document);
                            await _context.SaveChangesAsync();
                            _fileUploadService.DeleteFile(document.FilePath);
                            return (false, embedError);
                        }

                   
                        Vector? vectorData = embedding != null ? new Vector(embedding.ToArray()) : null;

                        var documentChunk = new DocumentChunk
                        {
                            DocumentId = document.Id,
                            Content = chunk,
                            Embedding = vectorData, // Gán trực tiếp kiểu Vector
                            ChunkOrder = chunkOrder++
                        };
                        // -----------------------------------

                        _context.DocumentChunks.Add(documentChunk);
                    }

                    document.IndexStatus = "Completed";
                    document.ErrorMessage = null;
                    _context.Documents.Update(document);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    document.IndexStatus = "Failed";
                    document.ErrorMessage = $"Indexing error: {ex.Message}";
                    _context.Documents.Update(document);
                    await _context.SaveChangesAsync();
                    _fileUploadService.DeleteFile(document.FilePath);
                    return (false, ex.Message);
                }
            }
        }
    }
}