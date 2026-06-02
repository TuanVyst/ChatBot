using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace ServiceLayer.Interfaces
{
    public interface IFileUploadService
    {
        Task<(bool success, string? filePath, string? errorMessage)> UploadFileAsync(
            Stream fileStream, string fileName);

        bool DeleteFile(string filePath);

        bool FileExists(string filePath);

        long GetFileSize(string filePath);
    }
}
