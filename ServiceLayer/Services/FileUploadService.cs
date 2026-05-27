using System;
using System.IO;
using System.Threading.Tasks;

namespace ServiceLayer.Services
{
    public class FileUploadService
    {
        private readonly string _uploadFolderPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".ppt", ".pptx" };

        public FileUploadService(string uploadFolderPath, long maxFileSize = 5242880) // 5MB default
        {
            _uploadFolderPath = uploadFolderPath;
            _maxFileSize = maxFileSize;

            // Ensure upload folder exists
            if (!Directory.Exists(_uploadFolderPath))
            {
                Directory.CreateDirectory(_uploadFolderPath);
            }
        }

        public async Task<(bool success, string? filePath, string? errorMessage)> UploadFileAsync(
            Stream fileStream, string fileName)
        {
            try
            {
                // Validate file extension
                var fileExtension = Path.GetExtension(fileName).ToLower();
                if (Array.IndexOf(_allowedExtensions, fileExtension) < 0)
                {
                    return (false, null, $"File type {fileExtension} not supported. Allowed: PDF, DOCX, PPT, PPTX");
                }

                // Validate file size
                if (fileStream.Length > _maxFileSize)
                {
                    return (false, null, $"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)} MB");
                }

                // Generate unique filename to avoid conflicts
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var filePath = Path.Combine(_uploadFolderPath, uniqueFileName);

                // Save file
                using (var fileToSave = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    await fileStream.CopyToAsync(fileToSave);
                }

                return (true, filePath, null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Upload failed: {ex.Message}");
            }
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete file {filePath}: {ex.Message}");
                return false;
            }
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public long GetFileSize(string filePath)
        {
            if (File.Exists(filePath))
            {
                return new FileInfo(filePath).Length;
            }
            return 0;
        }
    }
}


