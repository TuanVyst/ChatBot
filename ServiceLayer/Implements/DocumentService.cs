using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceLayer.Implements
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ISubjectRepository _subjectRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly IServiceScopeFactory _scopeFactory;

        public DocumentService(
            IDocumentRepository documentRepository,
            ISubjectRepository subjectRepository,
            IDocumentChunkRepository documentChunkRepository,
            IFileUploadService fileUploadService,
            IChapterRepository chapterRepository,
            IServiceScopeFactory scopeFactory)
        {
            _documentRepository = documentRepository;
            _subjectRepository = subjectRepository;
            _chapterRepository = chapterRepository;
            _documentChunkRepository = documentChunkRepository;
            _fileUploadService = fileUploadService;
            _scopeFactory = scopeFactory;
        }

        public async Task<(bool Success, string Message, int DocumentId)> UploadDocumentAsync(
            IFormFile file,
            string subjectId,
            string? chapterId)
        {
            if (file == null || file.Length == 0)
                return (false, "Vui lòng chọn file tải lên.", 0);

            if (string.IsNullOrWhiteSpace(subjectId))
                return (false, "ID môn học không được để trống.", 0);
            
            // Try to find subject by Name if it's not a GUID, assuming subjectId might be Name from the frontend
            Subject? subject = null;
            if (Guid.TryParse(subjectId, out var parsedSubjectId))
            {
                subject = await _subjectRepository.GetByIdAsync(subjectId);
            }
            else
            {
                 // subjectId is actually subjectName
                 var subjects = await _subjectRepository.GetAllAsync();
                 subject = subjects.FirstOrDefault(s => s.Name == subjectId);
            }
            
            if (subject == null)
                return (false, "Môn học không tồn tại.", 0);

            var existed = await _documentRepository.ExistsAsync(
                file.FileName,
                subject.Id);

            if (existed)
            {
                return (false, "Tài liệu này đã tồn tại trong môn học.", 0);
            }

            using var stream = file.OpenReadStream();

            var (uploadSuccess, filePath, uploadError) =
                await _fileUploadService.UploadFileAsync(stream, file.FileName);

            if (!uploadSuccess || string.IsNullOrEmpty(filePath))
                return (false, $"Lỗi lưu file: {uploadError}", 0);

            var fileSize = _fileUploadService.GetFileSize(filePath);
            var chapter = await _chapterRepository.GetByIdAsync(chapterId);
            if (chapter == null)
            {
                _fileUploadService.DeleteFile(filePath);
                return (false, "Chương không tồn tại hoặc không hợp lệ.", 0);
            }


            var document = new Document
            {
                FileName = file.FileName,
                FilePath = filePath,
                FileSize = fileSize,
                SubjectId = subject.Id,
                ChapterId = chapter.Id,
                IndexStatus = "Pending",
                UploadDate = DateTime.UtcNow
            };

            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();

            // Run Indexing in background thread
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var indexingService = scope.ServiceProvider.GetRequiredService<IIndexingService>();
                // We need to fetch the document fresh inside the new scope to avoid DbContext issues
                var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var docToProcess = await docRepo.GetByIdAsync(document.Id);
                if (docToProcess != null)
                {
                    await indexingService.IndexDocumentAsync(docToProcess);
                }
            });

            return (true, "Tải lên thành công, đang xử lý dữ liệu AI...", document.Id);
        }

        public async Task<IEnumerable<Document>> GetDocumentsAsync(string subjectId, string? chapterId = null)
        {
            return await _documentRepository.GetCompletedDocumentsAsync(subjectId, chapterId);
        }

        public async Task<Document?> GetByIdAsync(int id)
        {
            return await _documentRepository.GetByIdAsync(id);
        }

        public async Task<(bool Success, string Message)> ReindexDocumentAsync(int id)
        {
            var document = await _documentRepository.GetByIdWithChunksAsync(id);

            if (document == null)
                return (false, "Tài liệu không tồn tại.");

            await _documentChunkRepository.DeleteByDocumentIdAsync(id);
            await _documentChunkRepository.SaveChangesAsync();

            // Run Indexing in background thread
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var indexingService = scope.ServiceProvider.GetRequiredService<IIndexingService>();
                var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var docToProcess = await docRepo.GetByIdAsync(id);
                if (docToProcess != null)
                {
                    await indexingService.IndexDocumentAsync(docToProcess);
                }
            });

            return (true, "Đang tiến hành tái chỉ mục trong nền.");
        }

        public async Task<(bool Success, string Message)> DeleteDocumentAsync(int id)
        {
            var document = await _documentRepository.GetByIdWithChunksAsync(id);
            if (document == null)
                return (false, "Tài liệu không tồn tại.");

            await _documentChunkRepository.DeleteByDocumentIdAsync(id);
            _fileUploadService.DeleteFile(document.FilePath);
            await _documentRepository.DeleteAsync(document);
            await _documentRepository.SaveChangesAsync();

            return (true, "Xóa tài liệu thành công.");
        }
    }
}