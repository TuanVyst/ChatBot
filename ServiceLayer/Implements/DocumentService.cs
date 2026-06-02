using BusinessObject.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using ServiceLayer.Interfaces;

namespace ServiceLayer.Implements
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly IIndexingService _indexingService;

        public DocumentService(
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            IFileUploadService fileUploadService,
            IIndexingService indexingService)
        {
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _fileUploadService = fileUploadService;
            _indexingService = indexingService;
        }

        public async Task<(bool Success, string Message, int DocumentId)> UploadDocumentAsync(
            IFormFile file,
            string subjectName,
            string chapterName)
        {
            if (file == null || file.Length == 0)
                return (false, "Vui lòng chọn file tải lên.", 0);

            if (string.IsNullOrWhiteSpace(subjectName))
                return (false, "Tên môn học không được để trống.", 0);

            using var stream = file.OpenReadStream();

            var (uploadSuccess, filePath, uploadError) =
                await _fileUploadService.UploadFileAsync(stream, file.FileName);

            if (!uploadSuccess || string.IsNullOrEmpty(filePath))
                return (false, $"Lỗi lưu file: {uploadError}", 0);

            var fileSize = _fileUploadService.GetFileSize(filePath);

            var document = new Document
            {
                FileName = file.FileName,
                FilePath = filePath,
                FileSize = fileSize,
                SubjectName = subjectName,
                ChapterName = chapterName,
                IndexStatus = "Pending",
                UploadDate = DateTime.UtcNow
            };

            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveChangesAsync();

            var (indexSuccess, indexError) =
                await _indexingService.IndexDocumentAsync(document);

            if (!indexSuccess)
                return (false, $"File đã tải lên nhưng lỗi khi xử lý AI: {indexError}", document.Id);

            return (true, "Tải lên và xử lý dữ liệu AI thành công!", document.Id);
        }

        public async Task<IEnumerable<Document>> GetDocumentsAsync(string subjectName)
        {
            return await _documentRepository.GetCompletedDocumentsAsync(subjectName);
        }

        public async Task<(bool Success, string Message)> ReindexDocumentAsync(int id)
        {
            var document = await _documentRepository.GetByIdWithChunksAsync(id);

            if (document == null)
                return (false, "Tài liệu không tồn tại.");


            await _documentChunkRepository.DeleteByDocumentIdAsync(id);
            await _documentChunkRepository.SaveChangesAsync();

            var (indexSuccess, indexError   ) = await _indexingService.IndexDocumentAsync(document);
                if (!indexSuccess)
            {
               
                return (false, $"Lỗi khi tái chỉ mục: {indexError}");
            }

            return (true, "Tái chỉ mục thành công.");
        }
    }
}